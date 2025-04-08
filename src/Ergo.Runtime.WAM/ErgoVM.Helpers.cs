namespace Ergo.Runtime.WAM;

using Ergo.Compiler.Emission;
using Ergo.Lang.Ast.WellKnown;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;

public partial class ErgoVM
{
    #region Ancillary Operations
    public bool backtrack()
    {
        fail = false;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(backtrack)}");
#endif
        if (B == BOTTOM_OF_STACK)
        {
            Trace.WriteLine("[WAM] backtrack: hit bottom of stack → FAIL FINAL");
            return fail_and_exit_program();
        }
        B0 = Stack[B + Stack[B] + 7];
        P = Stack[B + Stack[B] + 4];
        Trace.WriteLine($"[WAM] Backtrack → P={P}, Code.Length={Code.Length}");
        Trace.WriteLine($"[WAM] Stack Snapshot @ B={B}:");
        for (int i = 0; i < 10; i++)
            Trace.WriteLine($"  Stack[{B + i}] = {Stack[B + i]}");
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

        while (cell.Tag == REF && cell.Value != addr)
        {
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
        if (t1 is (REF, _) && (t2 is not (REF, _) || a1 < a2))
        {
#if WAM_TRACE
            Trace.WriteLine($"[WAM] {nameof(bind)}: Store[a1]=Store[a2] {Store[a1]}={Store[a2]}");
#endif
            Store[a1] = Store[a2];
            trail(a1);
        }
        else
        {
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
        for (var i = a; i < b; ++i)
        {
            var addr = Trail[i];
            Store[addr] = (Term)(REF, addr);
            Trace.WriteLine($"[WAM] UNWIND addr={i}, resetting to REF {i}");
        }
    }
    public void tidy_trail()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] tidy_trail");
#endif
        var i = Stack[B + Stack[B] + 5];
        while (i < TR)
        {
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

        while (todo.Count > 0)
        {
            var (u, v) = todo.Pop();
            if (u == v) continue;

            var x = (Term)Store[u];
            var y = (Term)Store[v];

            if (x.Tag == REF || y.Tag == REF)
            {
                bind(u, v);
                continue;
            }

            if (x.Tag != y.Tag)
            {
                fail = true;
                return;
            }

            switch (x.Tag)
            {
                case CON:
                    if (x.Value != y.Value)
                    {
                        fail = true;
                        return;
                    }
                    break;

                case STR:
                    var f1 = Store[x.Value];
                    var f2 = Store[y.Value];
                    if (!Equals(f1, f2))
                    {
                        fail = true;
                        return;
                    }

                    var arity = ((Signature)f1).N;
                    for (int i = 1; i <= arity; ++i)
                        todo.Push((x.Value + i, y.Value + i));
                    break;

                case LIS:
                    todo.Push((x.Value, y.Value));
                    todo.Push((x.Value + 1, y.Value + 1));
                    break;

                default:
                    fail = true;
                    return;
            }
        }
    }

    public Lang.Ast.Term ReadHeapTerm(__ADDR addr)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] ReadHeapTerm addr={addr}");
#endif
        var term = (Term)Store[addr];
        return Read(term);

        Lang.Ast.Term Read(Term term)
        {
            return term.Tag switch
            {
                REF => new Lang.Ast.Variable($"_{term.Value}"),
                CON => Constants[term.Value],
                STR => ReadStructure(term.Value),
                LIS => ReadList(term.Value),
                _ => throw new NotSupportedException($"Tag {term.Tag} not supported")
            };
        }

        Lang.Ast.Term ReadStructure(__ADDR addr)
        {
            var functor = (Signature)Heap[addr]; // e.g. likes/2
            var args = new Lang.Ast.Term[functor.N];

            for (int i = 0; i < functor.N; i++)
                args[i] = Read(Heap[addr + 1 + i]);

            return new Lang.Ast.Complex(Constants[functor.F], args);
        }

        Lang.Ast.Term ReadList(__ADDR addr)
        {
            var elements = new List<Lang.Ast.Term>();
            Lang.Ast.Term tail = Collections.List.EmptyElement;
            while (true)
            {
                var headTerm = (Term)Heap[addr];
                var tailTerm = (Term)Heap[addr + 1];

                elements.Add(Read(headTerm));

                if (tailTerm.Tag == REF)
                {
                    // Unbound tail – this is a partial list
                    elements.Add(Read(tailTerm));
                    break;
                }

                if (tailTerm.Tag != LIS)
                {
                    // Proper end (e.g., []) or structure
                    tail = Read(tailTerm);
                    break;
                }

                addr = tailTerm.Value;
            }

            return new Lang.Ast.List(elements, tail);
        }
    }

    #endregion
}