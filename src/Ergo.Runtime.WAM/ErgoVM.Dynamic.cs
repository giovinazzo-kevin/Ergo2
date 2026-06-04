using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Signature = Ergo.Compiler.Emission.Signature;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Dynamic Predicates
    private readonly Dictionary<__WORD, DynamicPredicate> _dynamics = [];
    private readonly List<DynContinuation> _dynConts = [];
    private int _globalGen;
    private Emitter? _emitter;
    private KnowledgeBaseBytecode? _kb;

    public void InitDynamic(Emitter emitter, KnowledgeBaseBytecode kb)
    {
        _emitter = emitter;
        _kb = kb;
    }

    public void RegisterDynamic(Signature sig)
    {
        _dynamics[sig.RawValue] = new DynamicPredicate();
    }

    /// <summary>
    /// Returns the Store address of argument register A[i].
    /// </summary>
    private static int ArgAddr(int i) => HEAP_SIZE + STACK_SIZE + i;

    /// <summary>
    /// Re-appends all live dynamic clause code to the current _QUERY bytecode.
    /// Called at the start of each Run to ensure offsets are valid.
    /// </summary>
    private void RehydrateDynamicCode()
    {
        foreach (var dyn in _dynamics.Values)
        {
            foreach (var clause in dyn.Clauses)
            {
                if (clause.ErasedGen == int.MaxValue)
                {
                    // Sync constants
                    foreach (var c in clause.NewConstants)
                        _QUERY.AddConstant(c);
                    // Append code and update offset
                    clause.Offset = _QUERY.AppendCode(clause.Code);
                }
            }
        }
    }

    public void AssertClause(int ai = 0, bool atEnd = true)
    {
        System.Diagnostics.Trace.WriteLine($"[DYN] AssertClause fired! ai={ai}");
        if (_emitter == null || _kb == null)
            throw new InvalidOperationException("Dynamic predicates not initialized. Call InitDynamic first.");

        // Read the heap term from A[ai] back to AST
        var addr = deref(ArgAddr(ai));
        var term = ReadHeapTerm(addr);

        // Determine the signature
        var clause = term as Clause;
        var head = clause?.Functor ?? term;
        var sig = head.GetSignature().GetOrThrow();

        // Compile the clause using KB's constant table
        var ctx = EmitterContext.From(_kb);
        var rawCode = _emitter.EmitDynamicClause(ctx, term);

        // Collect new constants
        var newConstants = ctx.NewConstants(_kb)
            .Select(nc => Atom.FromObject(nc.Value))
            .ToArray();

        // Sync constants to KB and current query
        foreach (var c in newConstants)
        {
            _kb.AddConstant(c);
            _QUERY.AddConstant(c);
        }

        // Append to current query for immediate availability
        var offset = _QUERY.AppendCode(rawCode);

        // Create DynClause
        var gen = ++_globalGen;
        var dynClause = new DynClause(rawCode, newConstants, gen) { Offset = offset };

        // Get or create dynamic predicate entry
        var p = _kb.AddConstant(sig.Functor);
        var packed = (Signature)(p, sig.Arity);
        if (!_dynamics.TryGetValue(packed.RawValue, out var dyn))
        {
            dyn = new DynamicPredicate();
            _dynamics[packed.RawValue] = dyn;
        }

        if (atEnd)
            dyn.Clauses.Add(dynClause);
        else
            dyn.Clauses.Insert(0, dynClause);
    }

    public void RetractClause(int ai = 0)
    {
        var addr = deref(ArgAddr(ai));
        var term = ReadHeapTerm(addr);
        var clause = term as Clause;
        var head = clause?.Functor ?? term;
        var sig = head.GetSignature().GetOrThrow();

        var p = _kb.AddConstant(sig.Functor);
        var packed = (Signature)(p, sig.Arity);
        if (!_dynamics.TryGetValue(packed.RawValue, out var dyn))
            return;

        // Find first live clause and mark erased
        // TODO: proper unification-based matching for retract
        for (int i = 0; i < dyn.Clauses.Count; i++)
        {
            if (dyn.Clauses[i].ErasedGen == int.MaxValue)
            {
                dyn.Clauses[i].ErasedGen = ++_globalGen;
                return;
            }
        }
    }

    /// <summary>
    /// Attempts to dispatch a call to a dynamic predicate.
    /// Returns true if the signature is dynamic, false to fall through to static.
    /// </summary>
    private bool TryCallDynamic(Signature sig)
    {
        if (!_dynamics.TryGetValue(sig.RawValue, out var dyn))
            return false;

        var goalGen = _globalGen;
        var visible = dyn.Visible(goalGen).ToArray();

        if (visible.Length == 0)
        {
            fail = true;
            return true;
        }

        // Save return info
        CP = P;
        N = sig.N;
        B0 = B;

        if (visible.Length > 1)
        {
            // Create dynamic continuation for retry
            var contIdx = _dynConts.Count;
            _dynConts.Add(new DynContinuation(sig.RawValue, 1, goalGen));

            // Create choice point with negative BP = dynamic continuation
            var n = sig.N;
            var newB = (E > B) ? E + envsize() + 2 : B + Store[B] + 8;
            Store[newB] = n;
            for (int i = 0; i < n; i++)
                Store[newB + 1 + i] = A[i];
            Store[newB + n + 1] = E;
            Store[newB + n + 2] = CP;
            Store[newB + n + 3] = B;
            Store[newB + n + 4] = -(contIdx + 1); // negative = dynamic continuation
            Store[newB + n + 5] = TR;
            Store[newB + n + 6] = H;
            Store[newB + n + 7] = B0;
            HB = H;
            B = newB;
        }

        P = visible[0].Offset;
        return true;
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

        if (cont.ClauseIndex == visible.Length - 1)
        {
            // Last clause — trust: remove choice point
            B = Store[B + n + 3];
            HB = B == BOTTOM_OF_STACK ? 0 : Store[B + Store[B] + 6];
        }
        else
        {
            // More clauses — update continuation
            _dynConts[contIdx] = cont with { ClauseIndex = cont.ClauseIndex + 1 };
        }

        P = visible[cont.ClauseIndex].Offset;
        fail = false;
        return true;
    }
    #endregion

    #region Dynamic Builtin Registration
    /// <summary>
    /// Registers assert/assertz/asserta/retract builtins and initializes
    /// the dynamic clause compilation infrastructure.
    /// </summary>
    public void RegisterDynamicBuiltIns(KnowledgeBase kb)
    {
        var emitter = new Emitter();
        InitDynamic(emitter, kb.Bytecode);

        RegisterBuiltIn(kb, "assert", 1, vm => vm.AssertClause(0, atEnd: true));
        RegisterBuiltIn(kb, "assertz", 1, vm => vm.AssertClause(0, atEnd: true));
        RegisterBuiltIn(kb, "asserta", 1, vm => vm.AssertClause(0, atEnd: false));
        RegisterBuiltIn(kb, "retract", 1, vm => vm.RetractClause(0));
    }
    #endregion
}
