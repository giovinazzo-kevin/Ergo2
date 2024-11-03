namespace Ergo.Language.Ast;

public abstract class Expression(Operator op, params Term[] args) : Complex(op.CanonicalFunctor, args)
{
    public readonly Operator Operator = op;
}
