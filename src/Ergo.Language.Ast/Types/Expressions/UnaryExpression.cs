namespace Ergo.Language.Ast;

public abstract class UnaryExpression(Operator op, Term arg) : Expression(op, arg)
{
    public readonly Term Arg = arg;
}
