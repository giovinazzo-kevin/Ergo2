using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class Goal(Clause parent, Term[] args) : CallGraph.Node<Clause>
{
    public override Clause Parent => parent;
    public readonly Term[] Args = args;
}
