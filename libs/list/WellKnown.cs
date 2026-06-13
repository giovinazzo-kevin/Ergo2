using Ergo.Lang.Ast;
using static Ergo.Lang.Ast.Operator.Type;

namespace Ergo.Libs.List;

public static class WellKnown
{
    public static readonly __string EmptyList = "[]";
    public static readonly __string Functor = "[|]";
    public static readonly Operator Operator = new(900, xfy, Functor);
    public static readonly TerminatedCollection Collection = new(EmptyList, "[", "]");
}
