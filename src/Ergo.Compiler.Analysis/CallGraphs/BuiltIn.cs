using Ergo.Language.Ast;

namespace Ergo.Compiler.Analysis;

public abstract class BuiltIn(Library parent) : CallGraph.Node<Library>
{
    public override Library Parent => parent;
    public abstract Signature Signature { get; }
}