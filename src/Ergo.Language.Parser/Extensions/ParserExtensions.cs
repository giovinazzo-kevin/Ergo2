using Ergo.Language.Ast;
using Ergo.Language.Ast.WellKnown;
using Ergo.Language.Lexer;
using Ergo.Shared.Types;

namespace Ergo.Language.Parser.Extensions;

public static class ParserExtensions
{
    public static Term Parenthesized(this Term term, bool isParenthesized = true)
    {
        term.IsParenthesized = isParenthesized;
        return term;
    }
    public static Maybe<U> Cast<T, U>(this Func<Maybe<T>> func) => func().Cast<U>();
}
