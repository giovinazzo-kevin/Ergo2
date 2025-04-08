using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Lang.Ast;

public class Query
{
    public readonly Term[] Goals;

    private Query(params Term[] goals)
    {
        Goals = goals;
    }

    public static implicit operator Query(Term term)
    {
        if (term is ConsExpression cons && cons.Operator == Operators.Conjunction)
            return new Query([.. cons.Contents]);
        return new Query(term);
    }

}