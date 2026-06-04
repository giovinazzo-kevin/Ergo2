using Ergo.Compiler.Emission;
using System.Diagnostics;
using static Ergo.Runtime.WAM.ErgoVM;

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
        var es = envsize();
        var newE = E > B
            ? E + es + 2
            : B + Store[B] + 8;
        Store[newE] = E;
        Store[newE + 1] = CP;
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
        CP = Store[E + 1];
        E = Store[E];
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
        if (TryCallDynamic(p))
        {
            _traceLevel++;
            _F = p.F;
            _N = p.N;
            return;
        }
        if (defined(p, out var a))
        {
            if (a < 0)
            {
                CP = P;
                N = p.N;
                B0 = B;
                BuiltIns[-(a + 1)](this);
                if (!fail) P = CP;
                return;
            }
            _traceLevel++;
            _F = p.F;
            _N = p.N;
#if WAM_TRACE
            Trace.WriteLine($"[WAM] {nameof(Call)}: {Constants[p.F]}/{p.N}");
#endif
            CP = P;
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
    /// If P is defined, then save the current Choice Point's
    /// address in B0 and continue execution with instruction 
    /// labeled P;
    /// otherwise, backtrack.
    /// </summary>
    public void Execute()
    {
        var p = __signature();
        if (TryCallDynamic(p))
            return;
        if (defined(p, out var a))
        {
            if (a < 0)
            {
                N = p.N;
                B0 = B;
                BuiltIns[-(a + 1)](this);
                if (!fail) P = CP;
                return;
            }
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
        _traceLevel--;
        _inDynClause = false;
        Trace.WriteLine($"Exit: ({_traceLevel}) {Constants[_F]}" +
            $"({string.Join(", ", Enumerable.Range(0, _N).Select(i => Pretty(A[i])))})");
#if WAM_TRACE
        Trace.WriteLine($"[WAM] Proceed: P={P}, CP={CP}, CodeLen={Code.Length}");
#endif
        P = CP;
    }

    public void EmitSolution()
    {
#if WAM_TRACE
        Trace.WriteLine("[WAM] EmitSolution");
        Trace.WriteLine($"[WAM] EmitSolution: fail={fail}, P={P}, B={B}, B0={B0}");
        for (int i = 0; i < _VARS.Count; i++)
        {
            Trace.WriteLine($"[WAM] A[{i}] = {A[i]}, deref = {deref(((Term)A[i]).Value)}, Store = {Store[deref(((Term)A[i]).Value)]}");
        }
#endif
        SolutionEmitted(this);
        P = Code.Length; // 💥 Prevent re-execution
    }

    public Solution MaterializeSolution()
    {
        int i = 0;
        var bindings = new Binding[_VARS.Count];
        foreach (var (name, index) in _VARS.Values.OrderBy(x => x.Index))
        {
            var term = (Term)A[index];
            // A may hold REF (normal), CON (after put_unsafe_value), or STR
            if (term.Tag == REF)
            {
                var addr = deref(term.Value);
                term = (Term)Store[addr];
            }
            Trace.WriteLine($"[WAM] VAR {name} (A[{index}]) → tag={term.Tag}, val={term.Value}");
            if (term.Tag == CON)
                bindings[i++] = new(name, Constants[term.Value]);
            else
                bindings[i++] = new(name, $"_{term.Value}");

        }
        return new(bindings);
    }
    #endregion

}
