using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public abstract class BuiltIn(Library parent) : CallGraph.Node<Library>
{
    public override Library Parent => parent;
    public abstract Signature Signature { get; }
    /// <summary>
    /// Runtime handler, typed as Delegate to avoid Analysis→Runtime dependency.
    /// Concrete subclasses in libs return the actual __op handler.
    /// </summary>
    public virtual Delegate? Handler => null;
}