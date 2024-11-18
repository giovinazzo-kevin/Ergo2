using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public abstract class Directive(Library parent, Signature sig, int precedence) : CallGraph.Node<Library>
{
    public readonly Signature Signature = sig;
    public override Library Parent => parent;
    public readonly int Precedence = precedence;

    public abstract void Execute(Module module, ReadOnlySpan<Term> args);
}
