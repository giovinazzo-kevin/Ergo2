using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public sealed class __double(double value) : Atom(typeof(__double), value)
{
    public static implicit operator __double(double n) => new(n);
    public override string Expl => $"{Value:0.###}"!.Parenthesized(IsParenthesized);
    public override Term Clone() => (__double)value;
}
