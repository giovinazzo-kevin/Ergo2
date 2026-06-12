using Ergo.Lang.Ast;
using Ergo.Shared.Types;

namespace Ergo.Lang.Parsing.WellKnown;

public static class Delegates
{
    public delegate Func<Maybe<Term>> Parse(Parser parser);
}
