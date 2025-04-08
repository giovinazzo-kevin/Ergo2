using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public class ConsExpression : BinaryExpression
{
    public readonly IEnumerable<Term> Contents;

    public readonly int Length;

    public ConsExpression(Func<Term, Term, ConsExpression> fold, Operator op, params IEnumerable<Term> contents)
        : base(op, contents.First(), FoldAndCount(fold, op, contents.Skip(1), out var length))
    {
        Length = length;
        Contents = contents;
    }

    public ConsExpression(Operator op, Term lhs, Term rhs)
        : base(op, lhs, rhs)
    {
        var contents = new List<Term>() { lhs };
        while (rhs is BinaryExpression { Operator: var oop, Lhs: var item, Rhs: var next }
            && oop.Equals(op))
        {
            contents.Add(item);
            rhs = next;
        }
        contents.Add(rhs);
        Contents = contents;
    }

    static Term FoldAndCount(Func<Term, Term, ConsExpression> fold, Operator op, IEnumerable<Term> args, out int length)
    {
        var l = 0;
        var ret = args.Reverse().Aggregate((a, b) =>
        {
            l++;
            return fold(a, b);
        });
        length = l;
        return ret!;
    }

    public override string Expl => $"{Lhs.Expl}{Operator.CanonicalFunctor.Value} {Rhs.Expl}".Parenthesized(IsParenthesized);
    public override Term Clone() => new ConsExpression(Operator, Lhs.Clone(), Rhs.Clone());
}
