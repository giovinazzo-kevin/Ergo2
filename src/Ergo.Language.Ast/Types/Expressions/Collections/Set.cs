using Ergo.Language.Ast.WellKnown;

namespace Ergo.Language.Ast;

public class Set(params IEnumerable<Term> Contents) : CollectionExpression(Fold, Collections.Set, Operators.Set, Contents.Distinct())
{
    static Set Fold(Term a, Term b) => a is Set s
        ? new(s.Contents.Append(b))
        : new(a, b);
}