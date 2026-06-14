using Ergo.Lang.Ast;

namespace Ergo.Libs.Dict;

public static class WellKnown
{
    public static readonly __string Functor = "dict";
    public static readonly Lang.Ast.Signature Signature = Functor / 2;
}
