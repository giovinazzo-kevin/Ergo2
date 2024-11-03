namespace Ergo.Language.Ast.WellKnown;

public static class Collections
{
    public static readonly Collection Tuple = new("(", ")");
    public static readonly Collection Set = new("{", "}");
    public static readonly TerminatedCollectionDef List = new(Literals.EmptyList,  "[", "]");
}