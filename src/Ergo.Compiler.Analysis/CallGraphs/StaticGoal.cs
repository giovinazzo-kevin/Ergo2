using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class StaticGoal(Clause parent, Predicate callee, params Term[] args) : Goal(parent, args)
{
    public readonly Predicate Callee = callee;
}