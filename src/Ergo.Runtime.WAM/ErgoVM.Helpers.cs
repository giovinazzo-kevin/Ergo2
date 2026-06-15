using System.Diagnostics;

using Ergo.Compiler.Emission;
using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Query Lifecycle
    private __ADDR _savedB;
    private __ADDR _savedH;
    private __ADDR _savedTR;
    private bool _queryOpen;

    public void open_query(Query query)
    {
        _QUERY = query;
        CP = int.MaxValue;
        if (_dynamics.Count > 0)
            rehydrate_dynamic_code();
        _dynConts.Clear();
        _inDynClause = false;
        P = _QUERY.Bytecode.QueryStart;
        E = HEAP_SIZE;
        B = HEAP_SIZE;
        H = 0;
        TR = 0;
        fail = false;
        _savedB = B;
        _savedH = H;
        _savedTR = TR;
        _queryOpen = true;
    }

    public bool next_solution()
    {
        if (fail) {
            if (backtrack())
                return false;
        }
        while (true) {
            if (fail) {
                if (backtrack())
                    return false;
                continue;
            }
            if (P >= Code.Length)
                return true;
            var op = __word();
            OP_TABLE[op](this);
        }
    }

    public void close_query()
    {
        if (!_queryOpen) return;
        unwind_trail(_savedTR, TR);
        TR = _savedTR;
        H = _savedH;
        B = _savedB;
        HB = B == BOTTOM_OF_STACK ? 0 : Store[B + Store[B] + 6];
        fail = false;
        _queryOpen = false;
    }

    public void cut_query()
    {
        if (!_queryOpen) return;
        B = _savedB;
        HB = B == BOTTOM_OF_STACK ? 0 : Store[B + Store[B] + 6];
        fail = false;
        _queryOpen = false;
    }

    public IEnumerable<Solution> findall(Query query)
    {
        open_query(query);
        while (next_solution()) {
            yield return materialize_solution();
            fail = true;
        }
        close_query();
    }

    public Solution materialize_solution()
    {
        int i = 0;
        var bindings = new Binding[_QUERY.Variables.Count];
        foreach (var (name, index) in _QUERY.Variables.Values.OrderBy(x => x.Index)) {
            var addr = HEAP_SIZE + STACK_SIZE + index; // A register store address
            var term = read_heap_term(addr);
#if WAM_TRACE
            Trace.WriteLine($"[WAM] VAR {name} (A[{index}]) ? {term.Expl}");
#endif
            bindings[i++] = new(name, term);
        }
        return new(bindings);
    }
    #endregion

    #region Ancillary Operations
    public bool backtrack()
    {
        fail = false;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(backtrack)}");
#endif
        if (B == BOTTOM_OF_STACK) {
#if WAM_TRACE
            Trace.WriteLine("[WAM] backtrack: hit bottom of stack ? FAIL FINAL");
#endif
            return fail_and_exit_program();
        }
        B0 = Store[B + Store[B] + 7];
        P = Store[B + Store[B] + 4];
        // Dynamic predicate retry: negative P encodes a continuation index
        if (P < 0) {
            var contIdx = -(P + 1);
            if (!retry_dynamic(contIdx)) {
                // No more dynamic clauses � remove choice point and continue
                var n = Store[B];
                B = Store[B + n + 3];
                HB = B == BOTTOM_OF_STACK ? 0 : Store[B + Store[B] + 6];
                return backtrack();
            }
        }
#if WAM_TRACE
        Trace.WriteLine($"[WAM] Backtrack ? P={P}, Code.Length={Code.Length}");
        Trace.WriteLine($"[WAM] Stack Snapshot @ B={B}:");
        for (int i = 0; i < 10; i++)
            Trace.WriteLine($"  Store[{B + i}] = {Store[B + i]}");
#endif
        return fail;
    }
    public bool fail_and_exit_program()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(fail_and_exit_program)}");
#endif
        P = Code.Length;
        return fail = true;
    }
    public __WORD deref(__WORD addr)
    {
        var cell = (Term)Store[addr];

        while (cell.Tag == REF && cell.Value != addr) {
            addr = cell.Value;
            cell = (Term)Store[addr];
        }

        return addr;
    }

    public void bind(__ADDR a1, __ADDR a2)
    {
        if (a1 == a2)
            return; // Don't bind a cell to itself, ever
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(bind)}: {a1}={a2}");
#endif
        var t1 = (Term)Store[a1]; var t2 = (Term)Store[a2];
        if (t1 is (REF, _) && (t2 is not (REF, _) || a1 < a2)) {
#if WAM_TRACE
            Trace.WriteLine($"[WAM] {nameof(bind)}: Store[a1]=Store[a2] {Store[a1]}={Store[a2]}");
#endif
            Store[a1] = Store[a2];
            trail(a1);
        } else {
#if WAM_TRACE
            Trace.WriteLine($"[WAM] {nameof(bind)}: Store[a2]=Store[a1] {Store[a2]}={Store[a1]}");
#endif
            Store[a2] = Store[a1];
            trail(a2);
        }
    }
    public void trail(__ADDR a)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] TRAIL addr={a}, store={((Term)Store[a]).Tag}/{((Term)Store[a]).Value}");
#endif
        if ((a < HB || (H < a && a < B)) && TR < Trail.Length)
            Trail[TR++] = a;
    }
    public void unwind_trail(__ADDR a, __ADDR b)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] unwind_trail a={a} b={b}");
#endif
        for (var i = a; i < b; ++i) {
            var addr = Trail[i];
            Store[addr] = (Term)(REF, addr);
#if WAM_TRACE
            Trace.WriteLine($"[WAM] UNWIND addr={i}, resetting to REF {i}");
#endif
        }
    }
    public void tidy_trail()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] tidy_trail");
#endif
        var i = Store[B + Store[B] + 5];
        while (i < TR) {
            if (Trail[i] < HB || (H < Trail[i] && Trail[i] < B))
                ++i;
            else
                Trail[i] = Trail[--TR];
        }
    }
    public void unify(__ADDR a1, __ADDR a2)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] unify a1={a1} a2={a2}");
#endif
        Stack<(int, int)> todo = new();
        todo.Push((deref(a1), deref(a2)));

        while (todo.Count > 0) {
            var (u, v) = todo.Pop();
            if (!match(u, v, todo)) return;
        }
    }

    public bool match(__ADDR u, __ADDR v, Stack<(int, int)> todo)
    {
        if (u == v) return true;

        var x = (Term)Store[u];
        var y = (Term)Store[v];

        if (x.Tag == REF || y.Tag == REF) {
            bind(u, v);
            return true;
        }

        if (x.Tag != y.Tag) {
            fail = true;
            return false;
        }

        switch (x.Tag) {
            case CON:
                if (x.Value != y.Value) {
                    fail = true;
                    return false;
                }
                break;

            case STR:
                return walk(x.Value, y.Value, todo);

            case ABS:
                if (_QUERY.Source.AbstractTerms.Count > 0) {
                    var xSig = Heap[x.Value];
                    var ySig = Heap[y.Value];
                    if (xSig != ySig) { fail = true; return false; }
                    if (_QUERY.Source.AbstractTerms.TryGetValue(xSig, out var abs)) {
                        ((WellKnown.Delegates.Unify)abs.Unify)(this, x.Value, y.Value, todo);
                        break;
                    }
                }
                throw new NotSupportedException();

            default:
                fail = true;
                return false;
        }

        return true;
    }

    public bool walk(__ADDR xAddr, __ADDR yAddr, Stack<(int, int)> todo)
    {
        var f1 = Store[xAddr];
        var f2 = Store[yAddr];
        if (!Equals(f1, f2)) {
            fail = true;
            return false;
        }
        var arity = ((Signature)f1).N;
        for (int i = 1; i <= arity; ++i)
            todo.Push((xAddr + i, yAddr + i));
        return true;
    }

    public string pretty(Term t, bool quoted = false)
    {
        if (t.Tag == REF) {
            var addr = deref(t.Value);
            t = (Term)Store[addr];
        }

        return t.Tag switch {
            CON => quoted ? Constants[t.Value].Expl : Constants[t.Value].Value.ToString()!,
            REF => $"_{t.Value}",
            STR => pretty_structure(t.Value, quoted),
            ABS => pretty_abstract(t.Value, quoted),
            _ => "<?>"
        };

        string pretty_structure(__ADDR addr, bool quoted = false)
        {
            var sig = (Signature)Heap[addr];
            var functor = quoted ? Constants[sig.F].Expl : Constants[sig.F].Value.ToString()!;
            if (sig.N == 0) return functor;
            var args = new string[sig.N];
            for (int i = 0; i < sig.N; i++)
                args[i] = pretty((Term)Heap[addr + 1 + i], quoted);
            return $"{functor}({string.Join(", ", args)})";
        }

        string pretty_abstract(__ADDR addr, bool quoted = false)
        {
            var sig = Heap[addr];
            if (_QUERY.Source.AbstractTerms.TryGetValue(sig, out var abs)) {
                return ((WellKnown.Delegates.Pretty)abs.Pretty)(this, addr, quoted);
            }
            throw new NotSupportedException($"No abstract term handler registered for signature {sig}");
        }
    }

    /// <summary>
    /// Compute the gap after an environment frame at E.
    /// Reads the saved CP to find the frame size from Code[CP-1].
    /// Falls back to current arity N when the saved CP is out of range
    /// (e.g., top-level query where CP = int.MaxValue).
    /// </summary>
    public int env_size()
    {
        var savedCP = Store[E + 1];
        if (savedCP > 0 && savedCP <= Code.Length)
            return Code[savedCP - 1];
        return Math.Max(N, 16);
    }
    #endregion
}
