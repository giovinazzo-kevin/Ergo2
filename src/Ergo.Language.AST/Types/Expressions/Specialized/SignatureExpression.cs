using Ergo.Language.Ast.WellKnown;

namespace Ergo.Language.Ast;

public sealed class SignatureExpression : BinaryExpression
{
    public new readonly Atom Functor;
    public new readonly __int Arity;

    public SignatureExpression(Atom functor, __int arity) : base(Operators.Division, functor, arity)
    {
        Functor = functor;
        Arity = arity;
    }
}
