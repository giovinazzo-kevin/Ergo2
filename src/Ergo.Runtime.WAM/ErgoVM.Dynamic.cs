using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Signature = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Dynamic Predicates
    private readonly Dictionary<__WORD, DynamicPredicate> _dynamics = [];
    private readonly List<DynContinuation> _dynConts = [];
    private int _globalGen;
    private readonly Emitter _emitter = new();
    
    
    private bool _inDynClause;

    public void RegisterDynamic(Signature sig)
    {
        _dynamics[sig.RawValue] = new DynamicPredicate();
    }

    public IReadOnlyDictionary<__WORD, DynamicPredicate> GetDynamicPredicates() => _dynamics;

    /// <summary>
    /// Declares a predicate as dynamic: registers it as a builtin whose
    /// handler dispatches through dynamic clause machinery.
    /// Equivalent to :- dynamic F/N.
    /// </summary>
    public void DeclareDynamic(KnowledgeBase kb, string functor, int arity)
    {
        var c = kb.Bytecode.AddConstant(new __string(functor));
        var raw = ((__WORD)(Signature)(c, arity));
        if (!_dynamics.ContainsKey(raw))
            _dynamics[raw] = new DynamicPredicate();
        if (!kb.Bytecode.Labels.ContainsKey(raw))
            kb.RegisterBuiltInLabel(functor, arity, (__op)(vm => vm.DispatchDynamic(raw)));
    }

    /// <summary>
    /// Builtin handler for dynamic predicates. Creates choice points
    /// and sets P to the first visible clause's code offset.
    /// </summary>
    private void DispatchDynamic(__WORD sigRaw)
    {
        if (!_dynamics.TryGetValue(sigRaw, out var dyn)) {
            fail = true;
            return;
        }
        var goalGen = _globalGen;
        var visible = dyn.Visible(goalGen).ToArray();
        if (visible.Length == 0) {
            fail = true;
            return;
        }
        Signature sig = sigRaw;
        var n = sig.N;
        // Create choice point for backtracking through clauses
        var contIdx = _dynConts.Count;
        _dynConts.Add(new DynContinuation(sigRaw, 1, goalGen));
        var newB = (E >= B) ? E + env_size() + 2 : B + Store[B] + 8;
        Store[newB] = n;
        for (int i = 0; i < n; i++)
            Store[newB + 1 + i] = A[i];
        Store[newB + n + 1] = E;
        Store[newB + n + 2] = CP;
        Store[newB + n + 3] = B;
        Store[newB + n + 4] = -(contIdx + 1);
        Store[newB + n + 5] = TR;
        Store[newB + n + 6] = H;
        Store[newB + n + 7] = B0;
        HB = H;
        B = newB;
        P = visible[0].Offset;
        _inDynClause = true;
    }

    /// <summary>
    /// Returns the Store address of argument register A[i].
    /// </summary>
    public static int ArgAddr(int i) => HEAP_SIZE + STACK_SIZE + i;

    /// <summary>
    /// Re-appends all live dynamic clause code to the current _QUERY bytecode.
    /// Called at the start of each Run to ensure offsets are valid.
    /// </summary>
    private void RehydrateDynamicCode()
    {
        foreach (var dyn in _dynamics.Values) {
            foreach (var clause in dyn.Clauses) {
                if (clause.ErasedGen == int.MaxValue) {
                    // Sync constants
                    foreach (var c in clause.NewConstants)
                        _QUERY.Bytecode.AddConstant(c);
                    // Append code and update offset
                    clause.Offset = _QUERY.Bytecode.AppendCode(clause.Code);
                }
            }
        }
    }

    public void AssertClause(int ai = 0, bool atEnd = true)
    {
        System.Diagnostics.Trace.WriteLine($"[DYN] AssertClause fired! ai={ai}");

        // Read the heap term from A[ai] back to AST
        var addr = deref(ArgAddr(ai));
        var term = read_heap_term(addr);

        // Determine the signature
        var clause = term as Clause;
        var head = clause?.Functor ?? term;
        var sig = head.GetSignature().GetOrThrow();

        // Compile the clause using KB's constant table
        var ctx = EmitterContext.From(_QUERY.Source.Bytecode);
        var rawCode = _emitter.EmitDynamicClause(ctx, term);

        // Collect new constants
        var newConstants = ctx.NewConstants(_QUERY.Source.Bytecode)
            .Select(nc => Atom.FromObject(nc.Value))
            .ToArray();

        // Sync constants to KB and current query
        foreach (var c in newConstants) {
            _QUERY.Source.Bytecode.AddConstant(c);
            _QUERY.Bytecode.AddConstant(c);
        }

        // Append to current query for immediate availability
        var offset = _QUERY.Bytecode.AppendCode(rawCode);

        // Create DynClause
        var gen = ++_globalGen;
        var dynClause = new DynClause(rawCode, newConstants, gen) { Offset = offset };

        // Get or create dynamic predicate entry
        var p = _QUERY.Source.Bytecode.AddConstant(sig.Functor);
        var arityVal = sig.Arity.TryGetValue(out var av) ? av : Signature.VARIADIC;
        var packed = (Signature)(p, arityVal);
        if (!_dynamics.ContainsKey(packed.RawValue))
            DeclareDynamic(_QUERY.Source, (string)sig.Functor.Value, arityVal);
        var dyn = _dynamics[packed.RawValue];

        if (atEnd)
            dyn.Clauses.Add(dynClause);
        else
            dyn.Clauses.Insert(0, dynClause);
    }

    public void RetractClause(int ai = 0)
    {
        var addr = deref(ArgAddr(ai));
        var t = (Term)Store[addr];
        if (t.Tag != Term.__TAG.STR) return;
        var sig = (Signature)Heap[t.Value];
        var atom = Constants[sig.F];
        var packed = (Signature)(_QUERY.Source.Bytecode.AddConstant(atom), sig.N);
        if (!_dynamics.TryGetValue(packed.RawValue, out var dyn)) return;

        var savedTR = TR; var savedH = H;
        foreach (var clause in dyn.Visible(_globalGen).ToArray()) {
            fail = false;
            unify(addr, DecompileClauseHead(clause.Code, packed));
            if (!fail) { clause.ErasedGen = ++_globalGen; return; }
            unwind_trail(savedTR, TR); TR = savedTR; H = savedH; fail = false;
        }
        fail = true;
    }

    private int DecompileClauseHead(__WORD[] code, Signature sig)
    {
        var fAddr = H;
        Heap[H++] = sig;
        var args = H;
        for (int i = 0; i < sig.N; i++, H++) Heap[H] = (Term)(Term.__TAG.REF, H);
        for (int pc = 0; pc < code.Length;) {
            switch ((OpCode)code[pc++]) {
                case OpCode.allocate: continue;
                case OpCode.get_constant: Heap[args + code[pc + 1]] = (Term)(Term.__TAG.CON, code[pc]); pc += 2; continue;
                case OpCode.get_variable: case OpCode.get_value: case OpCode.get_structure: pc += 2; continue;
                case OpCode.get_level: pc++; continue;
                default: goto done;
            }
        }
    done:
        Heap[H++] = (Term)(Term.__TAG.STR, fAddr);
        return H - 1;
    }


    /// <summary>
    /// Handles backtracking into a dynamic predicate's next clause.
    /// Called when backtrack reads a negative P (dynamic continuation index).
    /// </summary>
    private bool RetryDynamic(int contIdx)
    {
        var cont = _dynConts[contIdx];
        var dyn = _dynamics[cont.Sig];
        var visible = dyn.Visible(cont.GoalGen).ToArray();

        if (cont.ClauseIndex >= visible.Length)
            return false; // No more clauses

        // Restore argument registers
        var n = Store[B];
        for (int i = 0; i < n; i++)
            A[i] = Store[B + 1 + i];

        // Restore registers
        E = Store[B + n + 1];
        CP = Store[B + n + 2];
        B0 = Store[B + n + 7];

        // Unwind trail
        var trailPoint = Store[B + n + 5];
        unwind_trail(trailPoint, TR);
        TR = trailPoint;

        // Reset heap
        H = Store[B + n + 6];
        HB = H;

        if (cont.ClauseIndex == visible.Length - 1) {
            // Last clause — trust: remove choice point
            B = Store[B + n + 3];
            HB = B == BOTTOM_OF_STACK ? 0 : Store[B + Store[B] + 6];
        } else {
            // More clauses — update continuation
            _dynConts[contIdx] = cont with { ClauseIndex = cont.ClauseIndex + 1 };
        }

        P = visible[cont.ClauseIndex].Offset;
        _inDynClause = true;
        fail = false;
        return true;
    }
    #endregion

}
