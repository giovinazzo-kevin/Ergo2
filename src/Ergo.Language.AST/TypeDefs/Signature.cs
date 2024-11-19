using Ergo.Shared.Interfaces;
using Ergo.Shared.Types;

namespace Ergo.Lang.Ast;

public readonly record struct Signature(Maybe<__string> Module, Atom Functor, int Arity) : IExplainable
{
    public Signature(Atom functor, int arity) : this(default, functor, arity) { }

    public string Expl => !Module.HasValue
        ? $"{Functor}/{Arity}"
        : $"{Module}:{Functor}/{Arity}";
    public override string ToString() => Expl;

    public Signature Unqualified => Module.HasValue ? this with { Module = default } : this;
}
