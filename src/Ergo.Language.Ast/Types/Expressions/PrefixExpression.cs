using Ergo.Shared.Extensions;

using static Ergo.Lang.Ast.Operator.Type;

namespace Ergo.Lang.Ast;

public class PrefixExpression(Operator op, Term arg) : UnaryExpression(op, arg)
{
    public override string Expl => (Operator.Type_ switch {
        fx => $"{Operator.CanonicalFunctor.Value} {Arg.Expl}",
        fy or _ => $"{Operator.CanonicalFunctor.Value}{Arg.Expl}",
    }).Parenthesized(IsParenthesized);
}

