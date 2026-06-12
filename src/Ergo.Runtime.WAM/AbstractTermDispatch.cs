using Ergo.Runtime.WAM.WellKnown;

namespace Ergo.Runtime.WAM;

public sealed class AbstractTermDispatch
{
    public readonly Delegates.Unify Unify;
    public readonly Delegates.Read Read;
    public readonly Delegates.WriteHeap WriteHeap;
    public readonly Delegates.Pretty Pretty;

    public AbstractTermDispatch(Ergo.Compiler.Analysis.AbstractTerm abs)
    {
        Unify = (Delegates.Unify)abs.Unify;
        Read = (Delegates.Read)abs.Read;
        WriteHeap = (Delegates.WriteHeap)abs.WriteHeap;
        Pretty = (Delegates.Pretty)abs.Pretty;
    }
}
