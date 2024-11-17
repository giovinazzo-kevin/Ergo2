using Ergo.Language.Ast;

namespace Ergo.Compiler.Analysis;

public class Clause(Predicate parent, Term[] args) : CallGraph.Node<Predicate>
{
    public override Predicate Parent => parent;
    public readonly Term[] Args = args;
    public readonly List<Goal> Goals = [];

    internal Clause WithGoals(IEnumerable<Goal> goals)
    {
        Goals.AddRange(goals);
        return this;
    }
}
