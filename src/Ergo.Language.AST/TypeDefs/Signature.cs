using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;

namespace Ergo.Lang.Ast;

public readonly record struct Signature(Maybe<__string> Module, Atom Functor, Maybe<int> Arity) : IExplainable
{
    public Signature(Atom functor, int arity) : this(default, functor, (Maybe<int>)arity) { }
    public Signature(Atom functor, Maybe<int> arity) : this(default, functor, arity) { }

    public string Expl => !Module.HasValue
        ? $"{Functor}/{(Arity.TryGetValue(out var a) ? a.ToString() : "*")}"
        : $"{Module}:{Functor}/{(Arity.TryGetValue(out var b) ? b.ToString() : "*")}";
    public override string ToString() => Expl;

    public Signature Unqualified => Module.HasValue ? this with { Module = default } : this;
}
