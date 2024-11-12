namespace Ergo.Language.Ast.Extensions;
public static class TermExtensions
{
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
    }
}
