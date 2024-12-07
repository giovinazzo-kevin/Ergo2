
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Lang.Ast;

public class Clause(Term head, Term body) : BinaryExpression(Operators.HornBinary, head, body)
{
    public new readonly Term Functor = head;
    public new readonly Term[] Args = head.GetArguments();

    public readonly IEnumerable<Term> Goals = 
        (body is BinaryExpression { IsCons: true } cons 
            && cons.Operator.Equals(Operators.Conjunction))
        ? new ConsExpression(cons.Operator, cons.Lhs, cons.Rhs).Contents
        : [body];

    public override string Expl => $"{Functor} {Operator.CanonicalFunctor.Value}\n{
        string.Join(",\n", Goals.Select(x => "    " + x.Expl))
    }";
}

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