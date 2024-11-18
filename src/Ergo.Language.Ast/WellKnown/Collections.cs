namespace Ergo.Lang.Ast.WellKnown;

public static class Collections
{
    public static readonly Collection Tuple = new("(", ")");
    public static readonly Collection Set = new("{", "}");
    public static readonly TerminatedCollection List = new(Literals.EmptyList,  "[", "]");
}