using Ergo.Shared.Types;

namespace Ergo.Lang.Parsing.Extensions;

public static class ParserExtensions
{
    public static Maybe<U> Cast<T, U>(this Func<Maybe<T>> func) => func().Cast<U>();
}
