
using Ergo.Compiler.Emission;
using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Ancillary Operations
    public bool backtrack()
    {
        fail = false;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(backtrack)}");
#endif
        if (B == BOTTOM_OF_STACK) {
#if WAM_TRACE
            Trace.WriteLine("[WAM] backtrack: hit bottom of stack → FAIL FINAL");
#endif
            return fail_and_exit_program();
        }
        B0 = Store[B + Store[B] + 7];
        P = Store[B + Store[B] + 4];
        // Dynamic predicate retry: negative P encodes a continuation index
        if (P < 0) {
            var contIdx = -(P + 1);
            if (!RetryDynamic(contIdx)) {
                // No more dynamic clauses — remove choice point and continue
                var n = Store[B];
                B = Store[B + n + 3];
                HB = B == BOTTOM_OF_STACK ? 0 : Store[B + Store[B] + 6];
                return backtrack();
            }
        }
#if WAM_TRACE
        Trace.WriteLine($"[WAM] Backtrack → P={P}, Code.Length={Code.Length}");
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
        return exit = fail = true;
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
            if (!MatchTerms(u, v, todo)) return;
        }
    }

    public bool MatchTerms(__ADDR u, __ADDR v, Stack<(int, int)> todo)
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
                return WalkStructure(x.Value, y.Value, todo);

            case ABS:
                if (_abstractTerms != null) {
                    var xSig = Heap[x.Value];
                    var ySig = Heap[y.Value];
                    if (xSig != ySig) { fail = true; return false; }
                    if (_abstractTerms.TryGetValue(xSig, out var abs)) {
                        abs.Unify(this, x.Value, y.Value, todo);
                        break;
                    }
                }
                // Fallback: list-style [head, tail] pairs after signature
                WalkList(x.Value + 1, y.Value + 1, todo);
                break;

            default:
                fail = true;
                return false;
        }

        return true;
    }

    public bool WalkStructure(__ADDR xAddr, __ADDR yAddr, Stack<(int, int)> todo)
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

    public void WalkList(__ADDR xAddr, __ADDR yAddr, Stack<(int, int)> todo)
    {
        todo.Push((xAddr, yAddr));
        todo.Push((xAddr + 1, yAddr + 1));
    }


    public Lang.Ast.Term ReadHeapTerm(__ADDR addr)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] ReadHeapTerm addr={addr}");
#endif
        addr = deref(addr);
        var term = (Term)Store[addr];
        return Read(term);

        Lang.Ast.Term Read(Term term)
        {
            // Follow REF chains for bound variables
            if (term.Tag == REF) {
                var a = deref(term.Value);
                var resolved = (Term)Store[a];
                if (resolved.Tag == REF && resolved.Value == a)
                    return new Lang.Ast.Variable($"_{a}"); // Unbound
                return Read(resolved);
            }
            return term.Tag switch {
                CON => Constants[term.Value],
                STR => ReadStructure(term.Value),
                ABS => ReadAbstract(term.Value),
                _ => throw new NotSupportedException($"Tag {term.Tag} not supported")
            };
        }

        Lang.Ast.Term ReadStructure(__ADDR addr)
        {
            var functor = (Signature)Heap[addr]; // e.g. likes/2
            var args = new Lang.Ast.Term[functor.N];

            for (int i = 0; i < functor.N; i++)
                args[i] = Read(Heap[addr + 1 + i]);

            var atom = Constants[functor.F];
            if (_reconstructors.TryGetValue((atom.Value, functor.N), out var reconstruct))
                return reconstruct(args);

            return new Lang.Ast.Complex(atom, args);
        }

        Lang.Ast.Term ReadList(__ADDR addr)
        {
            var elements = new List<Lang.Ast.Term>();
            Lang.Ast.Term tail = Collections.List.EmptyElement;
            while (true) {
                var headTerm = (Term)Heap[addr];
                var tailTerm = (Term)Heap[addr + 1];

                elements.Add(Read(headTerm));

                if (tailTerm.Tag == REF) {
                    elements.Add(Read(tailTerm));
                    break;
                }

                if (tailTerm.Tag != ABS) {
                    tail = Read(tailTerm);
                    break;
                }

                // Skip signature word in next cons cell
                addr = tailTerm.Value + 1;
            }

            return new Lang.Ast.List(elements, tail);
        }

        Lang.Ast.Term ReadAbstract(__ADDR addr)
        {
            var sig = Heap[addr];
            if (_abstractTerms != null && _abstractTerms.TryGetValue(sig, out var abs)) {
                return abs.Read(this, addr);
            }
            // Fallback: treat as list (data starts after signature)
            return ReadList(addr + 1);
        }
    }

    public string Pretty(Term t, bool quoted = false)
    {
        if (t.Tag == REF) {
            var addr = deref(t.Value);
            t = (Term)Store[addr];
        }

        return t.Tag switch {
            CON => quoted ? Constants[t.Value].Expl : Constants[t.Value].Value.ToString()!,
            REF => $"_{t.Value}",
            STR => PrettyStructure(t.Value, quoted),
            ABS => PrettyAbstract(t.Value, quoted),
            _ => "<?>"
        };
    }

    private string PrettyStructure(__ADDR addr, bool quoted = false)
    {
        var sig = (Signature)Heap[addr];
        var functor = quoted ? Constants[sig.F].Expl : Constants[sig.F].Value.ToString()!;
        if (sig.N == 0) return functor;
        var args = new string[sig.N];
        for (int i = 0; i < sig.N; i++)
            args[i] = Pretty((Term)Heap[addr + 1 + i], quoted);
        return $"{functor}({string.Join(", ", args)})";
    }

    private string PrettyList(__ADDR addr, bool quoted = false)
    {
        var elems = new List<string>();
        while (true) {
            var head = (Term)Heap[addr];
            var tail = (Term)Heap[addr + 1];
            elems.Add(Pretty(head, quoted));
            if (tail.Tag == CON && Constants[tail.Value].Value is string s && s == "[]")
                break;
            if (tail.Tag == ABS) {
                addr = tail.Value + 1; // skip signature
                continue;
            }
            if (tail.Tag == REF) {
                var d = deref(tail.Value);
                var dt = (Term)Store[d];
                if (dt.Tag == ABS) { addr = dt.Value + 1; continue; }
                if (dt.Tag == CON && Constants[dt.Value].Value is string s2 && s2 == "[]") break;
                elems.Add("|" + Pretty(dt, quoted));
                break;
            }
            elems.Add("|" + Pretty(tail, quoted));
            break;
        }
        return $"[{string.Join(",", elems)}]";
    }

    private string PrettyAbstract(__ADDR addr, bool quoted = false)
    {
        var sig = Heap[addr];
        if (_abstractTerms != null && _abstractTerms.TryGetValue(sig, out var abs)) {
            return abs.Pretty(this, addr, quoted);
        }
        // Fallback: list-style pretty after signature
        return PrettyList(addr + 1, quoted);
    }

    /// <summary>
    /// Compute the gap after an environment frame at E.
    /// Reads the saved CP to find the frame size from Code[CP-1].
    /// Falls back to current arity N when the saved CP is out of range
    /// (e.g., top-level query where CP = int.MaxValue).
    /// </summary>
    public int envsize()
    {
        var savedCP = Store[E + 1];
        if (savedCP > 0 && savedCP <= Code.Length)
            return Code[savedCP - 1];
        return Math.Max(N, 16);
    }
    #endregion
}