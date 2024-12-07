using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class Clause(Predicate parent, Term[] args) : CallGraph.Node<Predicate>
{
    public override Predicate Parent => parent;
    public readonly Term[] Args = args;
    public readonly List<Goal> Goals = [];

    public bool NeedsStackFrame => Args.Any(x => !x.IsGround) || Goals.Count > 0;

    public bool IsRecursive => Goals.Any(x
        => x is StaticGoal { Callee: var callee } && callee == parent
    );
    public bool IsTailRecursive => Goals.Count > 0 && Goals.Last()
        is StaticGoal { Callee: var callee } && callee == parent
    ;

    internal Clause WithGoals(IEnumerable<Goal> goals)
    {
        Goals.AddRange(goals);
        return this;
    }

    public bool TryGetVariable(Variable v, out byte i)
    {
        for (i = 0; i < Args.Length; i++)
        {
            if (Args[i] == v)
                return true;
        }
        return false;
    }
}
