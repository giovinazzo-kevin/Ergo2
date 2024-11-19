using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public class Complex(Atom functor, params Term[] args) : Term
{
    public readonly Atom Functor = functor;
    public readonly Term[] Args = args;
    public int Arity => Args.Length;
    public override bool IsGround => Args.All(x => x.IsGround);
    public override string Expl => ExplCanonical;
    public string ExplCanonical => $"{Functor.Expl}({string.Join((string)Functors.Comma.Value, Args.Select(x => x is Complex c ? c.ExplCanonical : x.Expl))})"
            .Parenthesized(IsParenthesized);
    public override bool Equals(object? obj) => obj is Complex { Functor: { } f, Args: { } a } && Functor.Equals(f);
    public override int GetHashCode() => Args.Prepend(Functor).Aggregate(0, HashCode.Combine);
}
