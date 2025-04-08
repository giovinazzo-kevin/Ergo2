using Ergo.Compiler.Emission;
using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Put Instructions
    /// <summary>
    /// Push a new unbound REF cell onto the heap and copy it
    /// into both register Xn and register Ai.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutVariableHeap()
    {
        var Xn = __word();
        var Ai = __word();
        Heap[H] = (Term)(REF, H);
    }
    /// <summary>
    /// Initialize the n-th stack variable in the current
    /// environment to 'unbound' and let Ai point to it.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutVariable()
    {
        var Yn = __word();
        var Ai = __word();
        var addr = E + Yn + 1;
        A[Ai] = Stack[addr] = (Term)(REF, __STACK(addr));
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(PutVariable)}: Yn={Yn} Ai={Ai} REF={__STACK(addr)}");
        Trace.WriteLine($"[DBG] E={E}, Yn={Yn}, addr={addr}, RAM[addr]={((Term)Store[__STACK(addr)]).Value}");
#endif

    }
    /// <summary>
    /// Place the contents of Vn into register Ai.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutValue()
    {
        var Vn = __word();
        var Ai = __word();
        V[Vn] = A[Ai];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(PutValue)}: Vn={Vn} Ai={Ai}");
#endif
    }
    /// <summary>
    /// If the dereferenced value of Yn is not an
    /// unbound stack variable in the current environment,
    /// set Ai to that value.
    /// Otherwise, bind the referenced stack variable
    /// to a new unbound variable cell pushed on the
    /// heap, and set Ai to point to that cell.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutUnsafeValue()
    {
        var Yn = __word();
        var Ai = __word();
        var addr = deref(E + Yn + 1);
        if (addr < E)
            A[Ai] = (Term)(REF, addr);
        else
        {
            Heap[H] = (Term)(REF, H);
            bind(addr, H);
            A[Ai] = Heap[H];
            H += 1;
        }
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(PutUnsafeValue)}: Yn={Yn} Ai={Ai} REF={addr}");
#endif
    }
    /// <summary>
    /// Push a new functor cell containing f onto the heap
    /// and set register Ai to an STR cell pointing to that
    /// functor cell.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutStructure()
    {
        var f = __signature();
        var Ai = __word();
        Heap[H] = f;
        A[Ai] = (Term)(STR, H);
        H += 1;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(PutStructure)}: f={Constants[f.F]}/{f.N} Ai={Ai}");
#endif
    }
    /// <summary>
    /// Set register Ai to contain a LIS cell pointing
    /// to the current top of the heap.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutList()
    {
        var Ai = __word();
        A[Ai] = (Term)(LIS, H);
        H += 1;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(PutList)}: Ai={Ai}");
#endif
    }
    /// <summary>
    /// Place a constant cell containing c into register Ai.
    /// Continue execution with the following instruction.
    /// </summary>
    public void PutConstant()
    {
        var c = (Term)(CON, __word());
        var Ai = __word();
        A[Ai] = c;
        H += 1;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(PutConstant)}: c={c} Ai={Ai}");
#endif
    }
    #endregion
}
