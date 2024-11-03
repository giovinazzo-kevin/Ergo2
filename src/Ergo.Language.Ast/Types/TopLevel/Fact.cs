
using Ergo.Language.Ast.WellKnown;

namespace Ergo.Language.Ast;
public class Fact(Term Head) : Clause(Head, Literals.True), ITopLevelTerm
{
    public override string Expl => $"{Head}";
}
