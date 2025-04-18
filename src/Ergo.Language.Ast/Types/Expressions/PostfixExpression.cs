﻿using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public class PostfixExpression(Operator op, Term arg) : UnaryExpression(op, arg)
{
    public override string Expl => (Operator.Type_ switch
    {
        _ => $"{Arg.Expl}{Operator.CanonicalFunctor.Value}",
    }).Parenthesized(IsParenthesized);
    public override Term Clone() => new PostfixExpression(Operator, Arg);
}
