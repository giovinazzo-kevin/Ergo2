using Ergo.Language.Ast.WellKnown;

namespace Ergo.Language.Ast;

public abstract class CollectionExpression(Func<Term, Term, CollectionExpression> fold, Collection col, Operator op, params IEnumerable<Term> items) 
    : ConsExpression(fold, op, items)
{
    public readonly Collection Collection = col;
    public override string Expl => $"{Collection.OpeningDelim}{string.Join((string)Functors.Comma.Value + ' ', Contents.Select(x => x.Expl))}{Collection.ClosingDelim}";
}
