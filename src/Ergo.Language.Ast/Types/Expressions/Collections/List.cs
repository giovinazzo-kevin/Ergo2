using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public class List : CollectionExpression
{
    private Term _tail;
    public Term Tail => _tail;
    public IEnumerable<Term> Head => Contents.SkipLast(1);

    public bool Complete => Tail.Equals(Collections.List.EmptyElement);
    public bool ListTail => Tail is List { } || Tail is Variable { Value: List { } };
    public int Count => Length + (Tail is List { } l ? l.Count : 1);

    public List(IEnumerable<Term> head, Term tail = null!) : base(Fold, Collections.List, Operators.List, Normalize(head, ref tail)) 
    {
        _tail = tail;
    }
    
    public static IEnumerable<Term> ExtractHead(BinaryExpression headTail)
    {
        if (headTail.Lhs is ConsExpression cons)
            return cons.Contents;
        if (headTail.Lhs is BinaryExpression { IsCons: true, Operator: var pOp } pseudoCons
            && pOp.Equals(Operators.Conjunction))
            return new ConsExpression(pseudoCons.Operator, pseudoCons.Lhs, pseudoCons.Rhs).Contents;
        return [headTail.Lhs];
    }

    static List Fold(Term a, Term b) => a is List l 
        ? new(l.Head.Prepend(b), l.Tail) 
        : new([b], a);

    static IEnumerable<Term> Normalize(IEnumerable<Term> head, ref Term tail)
    {
        tail ??= Collections.List.EmptyElement;
        if (Collections.List.EmptyElement.Equals(tail))
            return head.Append(tail);
        if (tail is List { Complete: true, Contents: var contents })
        {
            tail = Literals.EmptyList;
            return head.Concat(contents);
        }
        var newContents = head;
        while (tail is List { Contents: var newHead, Tail: var newTail })
        {
            tail = newTail;
            newContents = newContents.Concat(newHead.SkipLast(1));
        }
        return newContents.Append(tail);
    }

    protected readonly string COMMA = (string)Functors.Comma.Value;
    public override string Expl => 
        (Tail is Variable { IsBound: false }
         ? $"{base.Expl[..^(Collection.ClosingDelim.Length+ COMMA.Length+Tail.Expl.Length+1)]}|{Tail.Expl}{Collection.ClosingDelim}"
        : Complete
         ? $"{base.Expl[..^(Collection.ClosingDelim.Length+ COMMA.Length+Tail.Expl.Length+1)]}{Collection.ClosingDelim}"
        : ListTail 
         ? $"{base.Expl[..^(Collection.ClosingDelim.Length+ COMMA.Length+Tail.Expl.Length+1)]}{COMMA}{Tail.Expl[Collection.OpeningDelim.Length..^Collection.ClosingDelim.Length]}{Collection.ClosingDelim}"
        : base.Expl).Parenthesized(IsParenthesized);
}
