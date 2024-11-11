using Ergo.Language.Ast;
using Ergo.Language.Ast.WellKnown;
using Ergo.Language.Lexer;
using Ergo.Shared.Types;

namespace Ergo.Language.Parser.Extensions;

public static class ParserExtensions
{
    public static Maybe<U> Cast<T, U>(this Func<Maybe<T>> func) => func().Cast<U>();
}
