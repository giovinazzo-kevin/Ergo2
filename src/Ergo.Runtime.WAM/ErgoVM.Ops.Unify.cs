namespace Ergo.Runtime.WAM;

using Ergo.Compiler.Emission;
using System.Runtime.ExceptionServices;
using static ErgoVM.GetMode;
public partial class ErgoVM
{
    #region Unify Instructions
    /// <summary>
    /// 
    /// </summary>
    protected void UnifyVariable()
    {
        var Vn = __word();
        switch (mode)
        {
            case read:
                V[Vn] = Heap[S];
                break;
            case write:
                V[Vn] = Heap[H] = (Term)(REF, H);
                H += 1;
                break;
        }
        S += 1;
    }
    /// <summary>
    /// 
    /// </summary>
    protected void UnifyValue()
    {
        var Vn = __word();
        switch (mode)
        {
            case read:
                unify(V[Vn], S);
                break;
            case write:
                Heap[H] = V[Vn];
                H += 1;
                break;
        }
        S += 1;
        if (fail)
            backtrack();
    }
    /// <summary>
    /// 
    /// </summary>
    protected void UnifyLocalValue()
    {
        var Vn = __word();
        switch (mode)
        {
            case read:
                unify(V[Vn], S);
                break;
            case write:
                var addr = deref(V[Vn]);
                if (addr < H)
                    Heap[H] = Heap[addr];
                else
                {
                    Heap[H] = (Term)(REF, H);
                    bind(addr, H);
                }
                H += 1;
                break;
        }
        S += 1;
        if (fail)
            backtrack();
    }
    /// <summary>
    /// 
    /// </summary>
    protected void UnifyConstant()
    {
        var c = __word();
        switch (mode)
        {
            case read:
                var addr = deref(S);
                var cell = (Term)Store[addr];
                if (cell is (REF, _))
                {
                    Store[addr] = (Term)(CON, c);
                    trail(addr);
                }
                else fail = cell is not (CON, var c1) || c != c1;
                break;
            case write:
                Heap[H] = (Term)(CON, c);
                H += 1;
                break;
        }
        if (fail)
            backtrack();
    }
    /// <summary>
    /// 
    /// </summary>
    protected void UnifyVoid()
    {
        var n = __word();
        switch (mode)
        {
            case read:
                S += n;
                break;
            case write:
                for (int i = H; i < H + n; i++)
                    Heap[i] = (Term)(REF, i);
                H += n;
                break;
        }
        if (fail)
            backtrack();
    }
    #endregion
}
