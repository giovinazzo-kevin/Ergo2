using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public abstract class AbstractTerm(Library parent) : CallGraph.Node<Library>
{
    public override Library Parent => parent;
    public abstract Signature Signature { get; }
    public abstract Delegate Unify { get; }
    public abstract Delegate Read { get; }
    public abstract Delegate WriteHeap { get; }
    public abstract Delegate Pretty { get; }
}
