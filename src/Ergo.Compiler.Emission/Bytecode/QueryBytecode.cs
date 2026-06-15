namespace Ergo.Compiler.Emission;

public class QueryBytecode(__WORD[] data, Lang.Ast.Atom[] constants, int queryStart, int queryEnd) : Bytecode(data, constants)
{
    public readonly int QueryStart = queryStart;
    public readonly int QueryEnd = queryEnd;
    public ReadOnlySpan<__WORD> Query => Code[QueryStart..QueryEnd];
}