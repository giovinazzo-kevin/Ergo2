using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class DynamicGoal(Clause parent, Atom callee, params Term[] args) : Goal(parent)
{
    public readonly Atom Callee = callee;
    public readonly Term[] Args = args;
}
