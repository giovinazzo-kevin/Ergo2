using Ergo.Language.Ast;

namespace Ergo.Compiler.Analysis;

public class StaticGoal(Clause parent, Predicate callee, params Term[] args) : Goal(parent)
{
    public readonly Predicate Callee = callee;
    public readonly Term[] Args = args;
}