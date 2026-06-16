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
}
