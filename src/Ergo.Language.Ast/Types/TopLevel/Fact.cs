
using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Lang.Ast;
public class Fact(Term Head) : Clause(Head, Literals.True)
{
    public override string Expl => $"{Functor}";
    public override Term Clone() => new Fact(Functor);
}
