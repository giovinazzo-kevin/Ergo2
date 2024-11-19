namespace Ergo.Runtime.WAM;

using Ergo.Compiler.Emission;
public partial class ErgoVM
{
    #region Set Instructions
    /// <summary>
    /// Push a new unbound REF cell onto the heap and copy it into variable Vn.
    /// Continue execution with the following instruction.
    /// </summary>
    protected void SetVariable()
    {
        var Vn = __word();
        V[Vn] = Heap[H] = (Term)(REF, H);
        H += 1;
    }
    /// <summary>
    /// </summary>
    protected void SetValue()
    {
        var Vn = __word();
        Heap[H] = V[Vn];
        H += 1;
    }
    /// <summary>
    /// </summary>
    protected void SetLocalValue()
    {
        var Vn = __word();
        var addr = deref(V[Vn]);
        if (addr < H)
            Heap[H] = Heap[addr];
        else
        {
            Heap[H] = (Term)(REF, H);
            bind(addr, H);
        }
        H += 1;
    }
    /// <summary>
    /// </summary>
    protected void SetConstant()
    {
        var c = __word();
        Heap[H] = (Term)(CON, c);
        H += 1;
    }
    /// <summary>
    /// </summary>
    protected void SetVoid()
    {
        var n = __word();
        for (int i = H; i < H + n; ++i)
            Heap[i] = (Term)(REF, i);
        H += n;
    }
    #endregion
}
