using Ergo.Compiler.Emission;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;
namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Indexing Instructions
    /// <summary>
    /// Jump to the instruction labeled, respectively,
    /// V, C, L or S, depending on whether the dereferenced
    /// value of argument register A1 is a variable, a constant,
    /// a non-empty list or a structure, respectively.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected void SwitchOnTerm()
    {
        var (v, c, l, s) = (__addr(), __addr(), __addr(), __addr());
        P = (Term)Store[deref(A[0])] switch
        {
            (REF, _) => v,
            (CON, _) => c,
            (LIS, _) => l,
            (STR, _) => s,
            _ => throw new NotSupportedException()
        };
    }
    /// <summary>
    /// The dereferenced value of register A1 being a constant,
    /// jump to the instruction associated to it in hash table T
    /// of size N. If the constant found in A1 is not in the table,
    /// backtrack.
    /// </summary>
    protected void SwitchOnConstant()
    {
        var (N, T) = (__word(), __word());
        var (tag, val) = (Term)Store[deref(A[0])];
        Debug.Assert(tag == CON);
        var (found, inst) = get_hash(val, T, N);
        if (found) P = inst;
        else backtrack();
    }
    /// <summary>
    /// The dereferenced value of register A1 being a structure,
    /// jump to the instruction associated to it in hash table T
    /// of size N. If the functor of the structure found in A1 
    /// is not in the table, backtrack.
    /// </summary>
    protected void SwitchOnStructure()
    {
        var (N, T) = (__word(), __word());
        var (tag, val) = (Term)Store[deref(A[0])];
        Debug.Assert(tag == CON);
        var (found, inst) = get_hash(val, T, N);
        if (found) P = inst;
        else backtrack();
    }
    #endregion
}
