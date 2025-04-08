using Ergo.Compiler.Emission;
using System.Diagnostics;

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
    public void Allocate()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(Allocate)}");
#endif
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
    public void Deallocate()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(Deallocate)}");
#endif
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
    public void Call()
    {
        var p = __signature();
        if (defined(p, out var a))
        {
#if WAM_TRACE
            Trace.WriteLine($"[WAM] {nameof(Call)}: {Constants[p.F]}/{p.N}");
#endif
            CP = P;
            N = p.N;
            B0 = B;
            P = a;
            mode = GetMode.read;
        }
        else
        {
#if DEBUG
            throw new Exception($"Undefined predicate: {Constants[p.F]}/{p.N}");
#else
            backtrack();
#endif
        }
    }
    /// <summary>
    /// If P is defined, then save the current Choice Point's
    /// address in B0 and continue execution with instruction 
    /// labeled P;
    /// otherwise, backtrack.
    /// </summary>
    public void Execute()
    {
        var p = __signature();
        if (defined(p, out var a))
        {
#if WAM_TRACE
            Trace.WriteLine($"[WAM] {nameof(Execute)}: {Constants[p.F]}/{p.N}");
#endif
            N = p.N;
            B0 = B;
            P = a;
        }
        else
        {
#if DEBUG
            throw new Exception($"Undefined predicate: {Constants[p.F]}/{p.N}");
#else
            backtrack();
#endif
        }
    }
    /// <summary>
    /// Continue execution at instruction whose address is
    /// indicated by the continuation register CP.
    /// </summary>
    public void Proceed()
    {
#if WAM_TRACE
        Trace.WriteLine($"[WAM] Proceed: P={P}, CodeLen={Code.Length}");
#endif

        P = CP;
    }
    public void EmitSolution()
    {
        SolutionEmitted(this);
        P = Code.Length; // 💥 Prevent re-execution
#if WAM_TRACE
        Trace.WriteLine("[WAM] EmitSolution");
#endif
    }

    public Solution MaterializeSolution()
    {
        int i = 0;
        var bindings = new Binding[_VARS.Count];
        foreach (var (name, index) in _VARS.Values.OrderBy(x => x.Index))
        {
            var term = (Term)A[index];
            var addr = deref(term.Value);
            var resolved = (Term)Store[addr];
            if (resolved.Tag == CON)
                bindings[i++] = new(name, Constants[resolved.Value]);
            else
                bindings[i++] = new(name, ReadHeapTerm(resolved.Value));

        }
        return new(bindings);
    }
    #endregion

}
