namespace Ergo.Runtime.WAM;

using Ergo.Compiler.Emission;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;

public partial class ErgoVM
{
    #region Ancillary Operations
    public void backtrack()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(backtrack)}");
#endif
        if (B == BOTTOM_OF_STACK)
        {
            fail_and_exit_program();
            return;
        }
        B0 = Stack[B + Stack[B] + 7];
        P = Stack[B + Stack[B] + 4];
    }
    public void fail_and_exit_program()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(fail_and_exit_program)}");
#endif
        fail = true;
        P = Code.Length;
    }
    public __ADDR deref(__ADDR a)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(deref)}: REF={a}");
#endif
        Term t = Store[a];
        if (t is (REF, var b) && b != a)
            return b;
        return a;
    }
    public void bind(__ADDR a1, __ADDR a2)
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(bind)}: {a1}={a2}");
#endif
        var t1 = (Term)Store[a1]; var t2 = (Term)Store[a2];
        if (t1 is (REF, _) && (t2 is not (REF, _) || a1 < a2))
        {
            Store[a1] = Store[a2];
            trail(a1);
        }
        else
        {
            Store[a2] = Store[a1];
            trail(a2);
        }
    }
    public void trail(__ADDR a)
    {
        if (a < HB || (H < a) && (a < B))
        {
            Trail[TR++] = a;
        }
    }
    public void unwind_trail(__ADDR a, __ADDR b)
    {
        for (var i = a; i < b; ++i)
            Store[Trail[i]] = (Term)(REF, Trail[i]);
    }
    public void tidy_trail()
    {
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

    }

    public static __ADDR __STACK(int offset) => HEAP_SIZE + offset;
    #endregion
}