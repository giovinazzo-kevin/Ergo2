using Ergo.Shared.Extensions;

namespace Ergo.Language.Ast;

public sealed class __double(double val) : Atom(typeof(__double), val)
{
    public static implicit operator __double(double n) => new(n);
    public override string Expl => $"{Value:0.###}"!.Parenthesized(IsParenthesized);
}
