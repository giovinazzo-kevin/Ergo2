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
    public static IEnumerable<Variable> GetVariables(this Term term)
    {
        if (term is Atom)
            yield break;
        if (term is Variable v)
            yield return v;
        if (term is Complex c)
            foreach (var vv in c.Args.SelectMany(GetVariables))
                yield return vv;
        else
            foreach (var vv in term.Variables)
                yield return vv;
    }
    public static Term[] GetArguments(this Term term)
    {
        if (term is Complex c)
            return c.Args;
        return term.Arguments;
    }
    public static Maybe<Signature> GetSignature(this Term term) => term switch {
        Atom a
            => new Signature(default, a, 0),
        Complex c
            => new Signature(default, c.Functor, c.Arity),
        _ => Maybe<Signature>.None
    };
}
