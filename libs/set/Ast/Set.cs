using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;

namespace Ergo.Libs.Set.Ast;

public class Set : CollectionExpression
{
    private readonly Term _tail;
    public Term Tail => _tail;
    public IEnumerable<Term> Head => Contents.SkipLast(1);

    public bool Complete => Tail.Equals(WellKnown.EmptySet);

    public Set(IEnumerable<Term> head, Term tail = null!) : base(Fold, WellKnown.Collection, WellKnown.Operator, Normalize(head, ref tail))
    {
        _tail = tail;
    }

    private static Set Fold(Term a, Term b) => a is Set s
        ? new(s.Head.Prepend(b), s.Tail)
        : new([b], a);

    private static IEnumerable<Term> Normalize(IEnumerable<Term> head, ref Term tail)
    {
        tail ??= WellKnown.EmptySet;
        if (WellKnown.EmptySet.Equals(tail))
            return head.Order().Distinct().Append(tail);
        if (tail is Set { Complete: true, Contents: var contents }) {
            tail = WellKnown.EmptySet;
            return head.Concat(contents.SkipLast(1)).Order().Distinct().Append(tail);
        }
        return head.Order().Distinct().Append(tail);
    }

    protected readonly string COMMA = (string)Functors.Comma.Value;
    public override string Expl =>
        (Tail is Variable { IsBound: false }
         ? $"{base.Expl[..^(Collection.ClosingDelim.Length + COMMA.Length + Tail.Expl.Length + 1)]}|{Tail.Expl}{Collection.ClosingDelim}"
        : Complete
         ? $"{base.Expl[..^(Collection.ClosingDelim.Length + COMMA.Length + Tail.Expl.Length + 1)]}{Collection.ClosingDelim}"
        : base.Expl).Parenthesized(IsParenthesized);
    public override Term Clone() => new Set(Head.Select(t => t.Clone()), Tail.Clone());
}
