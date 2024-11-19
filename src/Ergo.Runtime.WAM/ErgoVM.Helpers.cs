namespace Ergo.Runtime.WAM;

using Ergo.Compiler.Emission;
using static Ergo.Compiler.Emission.Term.__TAG;

public partial class ErgoVM
{
    #region Ancillary Operations
    protected void backtrack()
    {
        if (B == BOTTOM_OF_STACK)
        {
            fail_and_exit_program();
            return;
        }
        B0 = Stack[B + Stack[B] + 7];
        P = Stack[B + Stack[B] + 4];
    }
    protected void fail_and_exit_program()
    {
        throw new NotImplementedException();
    }
    protected __ADDR deref(__ADDR a)
    {
        Term t = Store[a];
        if (t is (REF, var b) && b != a)
            return b;
        return a;
    }
    protected void bind(__ADDR a1, __ADDR a2)
    {
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
    protected void trail(__ADDR a)
    {
        if (a < HB || (H < a) && (a < B))
        {
            Trail[TR++] = a;
        }
    }
    protected void unwind_trail(__ADDR a, __ADDR b)
    {
        for (var i = a; i < b; ++i)
            Store[Trail[i]] = (Term)(REF, Trail[i]);
    }
    protected void tidy_trail()
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
    protected void unify(__ADDR a1, __ADDR a2)
    {

    }
    #endregion
}