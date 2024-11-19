using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class DynamicGoal(Clause parent, Atom callee, params Term[] args) : Goal(parent, args)
{
    public readonly Atom Callee = callee;
}
