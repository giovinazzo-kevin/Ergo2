using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Lang.Ast;

public class Set(params IEnumerable<Term> contents) : CollectionExpression(Fold, Collections.Set, Operators.Set, contents.Distinct())
{
    static Set Fold(Term a, Term b) => a is Set s
        ? new(s.Contents.Append(b))
        : new(a, b);
    public override Term Clone() => new Set(Contents.Select(c => c.Clone()));
}