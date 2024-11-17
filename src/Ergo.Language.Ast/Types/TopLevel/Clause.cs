
using Ergo.Language.Ast.Extensions;
using Ergo.Language.Ast.WellKnown;

namespace Ergo.Language.Ast;

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
