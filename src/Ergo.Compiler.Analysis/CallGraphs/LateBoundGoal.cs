using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class LateBoundGoal(Clause parent, Variable callee) : Goal(parent, [])
{
    public readonly Variable Callee = callee;
}