
using Ergo.Language.Ast.WellKnown;

namespace Ergo.Language.Ast;

public class Directive(Term arg) : PrefixExpression(Operators.HornUnary, arg), ITopLevelTerm
{
    public Term Head => Functor;
    public new Atom Functor => Arg switch
    {
        Atom a => a,
        Complex c => c.Functor,
        _ => throw new NotSupportedException()
    };

    public new Term[] Args => Arg switch
    {
        Atom a => [],
        Complex c => c.Args,
        _ => throw new NotSupportedException()
    };

    public new int Arity => Args.Length;

public override string Expl => $"{Operator.CanonicalFunctor.Value} {Arg}";
}
