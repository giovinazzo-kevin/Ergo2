namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Control Instructions
    /// <summary>
    /// Allocate a new environment on the stack, setting its
    /// Continuation Environment and Continuation Point fields
    /// to current E and CP, respectively. 
    /// Continue execution with the following instruction.
    /// </summary>
    protected void Allocate()
    {
        var newE = E > B
            ? E + Code[Stack[E + 1] - 1] + 2
            : B + Stack[B] + 8;
        Stack[newE] = E;
        Stack[newE + 1] = CP;
        E = newE;
    }
    /// <summary>
    /// Remove the environment frame at stack location E
    /// from the stack by resetting E to the value of its
    /// CE field and the continuation pointer CP to the
    /// value of its CP field.
    /// Continue execution with the following instruction.
    /// </summary>
    protected void Deallocate()
    {
        CP = Stack[E + 1];
        E = Stack[E];
    }
    /// <summary>
    /// If P is defined, then save the current Choice Point's
    /// address in B0 and the value of current continuation in
    /// CP, and continue execution with instruction labeled P,
    /// with N stack variables remaining in the current Env.;
    /// otherwise, backtrack.
    /// </summary>
    protected void Call()
    {
        var p = __signature();
        if (defined(p, out var a))
        {
            CP = P;
            N = p.N;
            B0 = B;
            P = a;
        }
        else
            backtrack();
    }
    /// <summary>
    /// If P is defined, then save the current Choice Point's
    /// address in B0 and continue execution with instruction 
    /// labeled P;
    /// otherwise, backtrack.
    /// </summary>
    protected void Execute()
    {
        var p = __signature();
        if (defined(p, out var a))
        {
            N = p.N;
            B0 = B;
            P = a;
        }
        else
            backtrack();
    }
    /// <summary>
    /// Continue execution at instruction whose address is
    /// indicated by the continuation register CP.
    /// </summary>
    protected void Proceed()
    {
        P = CP;
    }
    #endregion
}
