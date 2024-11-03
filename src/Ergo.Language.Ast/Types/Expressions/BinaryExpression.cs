namespace Ergo.Language.Ast;

using Ergo.Language.Ast.WellKnown;
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
            var cmp = op.Precedence.CompareTo(lhs.Operator.Precedence);
            var ret = cmp switch
            {
                > 0 => B(op, lhs, rhs),
                < 0 => B(op, lhs, rhs),
                0 => (op.Associativity_, lhs.Operator.Associativity_) switch
                {
                    (Left, Left) or (Left, None) => B(op, Associate(B(lhs.Operator, lhs.Lhs, lhs.Rhs)), rhs),
                    _ => B(op, lhs, rhs)
                }
            };
            return ret;
        }

        BinaryExpression RhsCase(Operator op, Term lhs, BinaryExpression rhs)
        {
            var cmp = op.Precedence.CompareTo(rhs.Operator.Precedence);
            var ret = cmp switch
            {
                > 0 => B(op, lhs, Associate(rhs)),
                < 0 => B(rhs.Operator, Associate(B(op, lhs, rhs.Lhs)), rhs.Rhs),
                0 => (op.Associativity_, rhs.Operator.Associativity_) switch
                {
                    (Left, Left) or (Left, None) => B(op, Associate(B(rhs.Operator, lhs, rhs.Lhs)), rhs.Rhs),
                    _ => B(op, lhs, rhs)
                }
            };
            return ret;
        }

        static BinaryExpression MixedCase(Operator op, BinaryExpression lhs, BinaryExpression rhs)
        {
            return B(op, lhs, rhs);
        }

        static BinaryExpression B(Operator op, Term lhs, Term rhs) => new(op, lhs, rhs);
    }

    public override string Expl => (Operator.CanonicalFunctor.Value switch
    {
        "," => $"{Lhs.Expl}{Operator.CanonicalFunctor.Value} {Rhs.Expl}",
        "|" => $"{Lhs.Expl}{Operator.CanonicalFunctor.Value}{Rhs.Expl}",
        _ => $"{Lhs.Expl} {Operator.CanonicalFunctor.Value} {Rhs.Expl}"
    }).Parenthesized(IsParenthesized);
}
