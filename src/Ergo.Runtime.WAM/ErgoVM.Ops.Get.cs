
using Ergo.Compiler.Emission;
using static Ergo.Runtime.WAM.ErgoVM.GetMode;

namespace Ergo.Runtime.WAM;

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
        Trace.WriteLine($"[WAM] GetVariable: {Vn} {Ai}");
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
        Trace.WriteLine($"[WAM] GetValue: {Vn} {Ai}");
#endif
        unify(HEAP_SIZE + STACK_SIZE + MAX_ARGS + Vn, HEAP_SIZE + STACK_SIZE + Ai);
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
        var term = (Term)A[Ai];
        __ADDR addr;
        if (term.Tag == REF) {
            addr = deref(term.Value);
            term = (Term)Store[addr];
        } else
            addr = -1; // not used for non-REF
        Term cell = term;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] GetStructure: {cell.Value} {Ai}");
#endif
        if (cell is (REF, _)) {
            Heap[H] = (Term)(STR, H + 1);
            Heap[H + 1] = f;
            bind(addr, H);
            H += 2;
            mode = write;
        } else if (cell is (STR, var a)) {
            if (Heap[a].Equals(f)) {
                S = a + 1;
                mode = read;
            } else fail = true;
        } else fail = true;
        if (fail)
            backtrack();
    }
    /// <summary>
    /// If the dereferenced value of register Ai is an 
    /// unbound variable, then bind that variable to a
    /// new ABS cell pushed on the heap with the given
    /// signature and set mode to write;
    /// otherwise, if it is an ABS cell with matching
    /// signature, then set register S to the heap address
    /// following the signature word and set mode to read.
    /// If it is not an ABS cell or signature doesn't match, fail.
    /// </summary>
    public void GetAbstract()
    {
        var sig = __word();
        var Ai = __word();
        var term = (Term)A[Ai];
        __ADDR addr;
        if (term.Tag == REF) {
            addr = deref(term.Value);
            term = (Term)Store[addr];
        } else
            addr = -1;
        var cell = term;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] GetAbstract: sig={sig} Ai={Ai}");
#endif
        if (cell is (REF, _)) {
            Heap[H] = (Term)(ABS, H + 1);
            Heap[H + 1] = sig;
            bind(addr, H);
            H += 2;
            mode = write;
        } else if (cell is (ABS, var a)) {
            if (Heap[a] == sig) {
                S = a + 1;
                mode = read;
            } else fail = true;
        } else fail = true;
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
        if (term.Tag == CON) {
            fail = term.Value != c;
        } else if (term.Tag == REF) {
            var addr = deref(term.Value);
            var resolved = (Term)Store[addr];
            if (resolved.Tag == REF && resolved.Value == addr) {
                // Unbound — bind to constant
                Store[addr] = (Term)(CON, c);
                trail(addr);
            } else if (resolved.Tag == CON) {
                // Already bound — check equality
                fail = resolved.Value != c;
            } else {
                fail = true;
            }
        } else {
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
