using System.Reflection.Emit;

namespace Ergo.Runtime.WAM;

using Ergo.Compiler.Emission;
using static ErgoVM.GetMode;
using static Compiler.Emission.Term.__TAG;
using System.Diagnostics;

public partial class ErgoVM
{
    #region Get Instructions
    /// <summary>
    /// Place the contents of register Ai into variable Vn.
    /// Continue execution with the following instruction.
    /// </summary>
    public void GetVariable()
    {
        var Vn = __word();
        var Ai = __word();
#if WAM_TRACE
        Trace.WriteLine($"GVAR: {Vn} {Ai}");
#endif
        V[Vn] = A[Ai];
    }
    /// <summary>
    /// Unify variable Vn and register Ai.
    /// Backtrack on failure, otherwise continue 
    /// execution with the following instruction.
    /// </summary>
    public void GetValue()
    {
        var Vn = __word();
        var Ai = __word();
#if WAM_TRACE
        Trace.WriteLine($"GVAL: {Vn} {Ai}");
#endif
        unify(Vn, Ai);
        if (fail) 
            backtrack();
    }
    /// <summary>
    /// If the dereferenced value of register Ai is an 
    /// unbound variable, then bind that variable to a
    /// new STR cell pointing to f pushed on the heap
    /// and set mode to write;
    /// otherwise, if it is an STR cell pointing to
    /// functor f, then set register S to the heap
    /// address following that functor's cell and set
    /// mode to read.
    /// If it is not a STR cell or if the functor is
    /// different than f, fail.
    /// Backtrack on failure, otherwise continue
    /// execution with the following instruction.
    /// </summary>
    public void GetStructure()
    {
        var f = __signature();
        var Ai = __addr();
        var addr = deref(A[Ai]);
        Term cell = Store[addr];
#if WAM_TRACE
        Trace.WriteLine($"GSTR: {cell.Value} {Ai}");
#endif
        if (cell is (REF, _))
        {
            Heap[H] = (Term)(STR, H + 1);
            Heap[H + 1] = f;
            bind(addr, H);
            H += 2;
            mode = write;
        }
        else if (cell is (STR, var a))
        {
            if (Heap[a].Equals(f)) {
                S = a + 1;
                mode = read;
            }
            else fail = true;
        }
        else fail = true;
        if (fail) 
            backtrack();
    }
    /// <summary>
    /// If the dereferenced value of register Ai is an 
    /// unbound variable, then bind that variable to a
    /// new LIS pushed on the heap and set mode to write;
    /// otherwise, if it is a LIS cell, then set register
    /// S to the heap address it contains and set mode to
    /// read. If it is not a LIS cell, fail.
    /// Backtrack on failure, otherwise continue
    /// execution with the following instruction.
    /// </summary>
    public void GetList()
    {
        var Ai = __word();
        var addr = deref(A[Ai]);
        var cell = (Term)Store[addr];
#if WAM_TRACE
        Trace.WriteLine($"GLIS: {cell.Value} {Ai}");
#endif
        if (cell is (REF, _))
        {
            Heap[H] = (Term)(LIS, H + 1);
            bind(addr, H);
            H += 1;
            mode = write;
        }
        else if (cell is (LIS, var a ))
        {
            S = a;
            mode = read;
        }
        else fail = true;
        if (fail)
            backtrack();
    }
    /// <summary>
    /// If the dereferenced value of register Ai is
    /// an unbound variable, then bind that variable
    /// to the constant c. Otherwise, fail if it is 
    /// not the constant c.
    /// Backtrack on failure, otherwise continue
    /// execution with the following instruction.
    /// </summary>
    public void GetConstant()
    {
        var c = __word();           // constant index
        var Ai = __word();
        var term = (Term)A[Ai];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(GetConstant)}: c={c} Ai={Ai}");
#endif
        if (term.Tag == CON)
        {
            fail = term.Value != c;
        }
        else if (term.Tag == REF)
        {
            Store[term.Value] = (Term)(CON, c);
            trail(term.Value);
        }
        else
        {
            var addr = deref(term.Value);
            var cell = (Term)Store[addr];
            fail = cell is not (CON, var c1) || c1 != c;
        }
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(GetConstant)}: fail={fail}");
#endif
        if (fail)
            backtrack();
    }
    #endregion
}
