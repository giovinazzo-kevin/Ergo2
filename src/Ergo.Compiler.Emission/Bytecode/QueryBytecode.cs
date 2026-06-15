namespace Ergo.Compiler.Emission;

public class QueryBytecode(__WORD[] data, Lang.Ast.Atom[] constants, int queryStart, int queryEnd) : Bytecode(data, constants)
{
    public static readonly QueryBytecode EMPTY = new([0, 0, 0], [], 0, 0);
    public static QueryBytecode Preloaded(__WORD[] code, Lang.Ast.Atom[]? constants = null) => new([0, 0, .. code], constants ?? [], 0, code.Length);

    public readonly int QueryStart = queryStart;
    public readonly int QueryEnd = queryEnd;
    public ReadOnlySpan<__WORD> Query => Code[QueryStart..QueryEnd];
}