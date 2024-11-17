using Ergo.Language.Ast;

namespace Ergo.Compiler.Analysis;

public class Goal(Clause parent) : CallGraph.Node<Clause>
{
    public override Clause Parent => parent;
}
