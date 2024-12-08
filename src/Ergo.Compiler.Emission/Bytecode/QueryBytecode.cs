using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ergo.Compiler.Emission;
public class QueryBytecode(__WORD[] data, Lang.Ast.Atom[] constants, int queryStart) : Bytecode(data, constants)
{
    public readonly int QueryStart = queryStart;
    public ReadOnlySpan<__WORD> Query => Code[QueryStart..];
}