using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Lang.Ast;

public class Set(params IEnumerable<Term> Contents) : CollectionExpression(Fold, Collections.Set, Operators.Set, Contents.Distinct())
{
    static Set Fold(Term a, Term b) => a is Set s
        ? new(s.Contents.Append(b))
        : new(a, b);
}