
using Ergo.Lang.Ast.WellKnown;

namespace Ergo.Lang.Ast;

public class Directive(Term arg) : PrefixExpression(Operators.HornUnary, arg)
{
    public new readonly Atom Functor = arg switch
    {
        Atom a => a,
        Complex c => c.Functor,
        _ => throw new NotSupportedException()
    };
    public new readonly Term[] Args = arg switch
    {
        Atom a => [],
        Complex c => c.Args,
        _ => throw new NotSupportedException()
    };
    public new int Arity => Args.Length;

    public override string Expl => $"{Operator.CanonicalFunctor.Value} {Arg}";
}
