namespace Ergo.Lang.Ast;

using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;
using static Operator.Associativity;
using static Operator.Fixity;
public class BinaryExpression(Operator op, Term lhs, Term rhs) : Expression(op, lhs, rhs)
{
    public readonly Term Lhs = lhs;
    public readonly Term Rhs = rhs;
    public bool IsCons =>
        Operator.Associativity_ == Right 
        && Operator.Fixity_ == Infix 
        && (!(Rhs is BinaryExpression { Operator: var op } x) || !op.Equals(Operator)
            || x.IsCons);
    public bool IsHeadTail =>
        Operator.Equals(Operators.Pipe) 
        && (Lhs is CollectionExpression 
            || Lhs is not BinaryExpression 
            || Lhs is BinaryExpression { Operator: var op, IsCons: true } 
            && op.Equals(Operators.Conjunction));

    public static BinaryExpression Associate(BinaryExpression exp)
    {
        var last = default(BinaryExpression);
        while (!exp.Equals(last))
        {
            last = exp;
            exp = AssociateOnce(exp);
        }
        return exp;
    }

    protected static BinaryExpression AssociateOnce(BinaryExpression exp)
    {
        var associated = (exp.Lhs, exp.Rhs) switch
        {
            (CollectionExpression, _) or (_, CollectionExpression) => exp,
            (BinaryExpression lhs, BinaryExpression rhs) => MixedCase(exp.Operator, lhs, rhs),
            (BinaryExpression lhs, Term rhs) => LhsCase(exp.Operator, lhs, rhs),
            (Term lhs, BinaryExpression rhs) => RhsCase(exp.Operator, lhs, rhs),
            _ => exp
        };
        return associated;

        BinaryExpression LhsCase(Operator op, BinaryExpression lhs, Term rhs)
        {
            return exp;
        }

        BinaryExpression RhsCase(Operator op, Term lhs, BinaryExpression rhs)
        {
            var cmp = op.Precedence.CompareTo(rhs.Operator.Precedence);
            var ret = cmp switch
            {
                > 0 => exp,
                < 0 => AssociateRhsLeft(),
                0 => (op.Associativity_, rhs.Operator.Associativity_) switch {
                    (Left, Left) or (Left, None) => AssociateRhsLeft(),
                    _ => exp
                }
            };
            return ret;

            BinaryExpression AssociateRhsLeft() => new(rhs.Operator, Associate(new(op, lhs, rhs.Lhs)), rhs.Rhs);
        }

        BinaryExpression MixedCase(Operator op, BinaryExpression lhs, BinaryExpression rhs)
        {
            return exp;
        }
    }

    public static BinaryExpression AddNecessaryParentheses(BinaryExpression exp)
    {
        if (exp.Lhs is BinaryExpression lexp)
        {
            AddNecessaryParentheses(lexp);
            if (lexp.Operator.Precedence > exp.Operator.Precedence)
                exp = new(exp.Operator, lexp.Parenthesized(), exp.Rhs);
        }
        if (exp.Rhs is BinaryExpression rexp)
        {
            AddNecessaryParentheses(rexp);
            if (rexp.Operator.Precedence > exp.Operator.Precedence)
                exp = new(exp.Operator, exp.Lhs, rexp.Parenthesized());
        }
        return exp;
    }

    public override string Expl => (Operator.CanonicalFunctor.Value switch
    {
        "," => $"{Lhs.Expl}{Operator.CanonicalFunctor.Value} {Rhs.Expl}",
        "|" => $"{Lhs.Expl}{Operator.CanonicalFunctor.Value}{Rhs.Expl}",
        _ => $"{Lhs.Expl} {Operator.CanonicalFunctor.Value} {Rhs.Expl}"
    }).Parenthesized(IsParenthesized);
}
