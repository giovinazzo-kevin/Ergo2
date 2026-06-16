using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Types;

namespace Ergo.Lang.Ast.Extensions;

public static class TermExtensions
{
    public static Term Deref(this Term term)
    {
        if (term is Variable v)
            return v.Value;
        return term;
    }

    public static Term Parenthesized(this Term term, bool isParenthesized = true)
    {
        term.IsParenthesized = isParenthesized;
        return term;
    }
    public static Term[] GetArguments(this Term term)
    {
        if (term is Complex c)
            return c.Args;
        return term.Args;
    }
    public static Maybe<Signature> GetSignature(this Term term) => term switch {
        Atom a
            => new Signature(default, a, 0),
        Complex c
            => new Signature(default, c.Functor, c.Arity),
        _ => Maybe<Signature>.None
    };
}
