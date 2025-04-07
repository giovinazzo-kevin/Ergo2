using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ergo.Compiler.Emission;
public class QueryBytecode(__WORD[] data, Lang.Ast.Atom[] constants, int queryStart) : Bytecode(data, constants)
{
    public static readonly QueryBytecode EMPTY = new ([0, 0, 0], [], 0);
    public static QueryBytecode Preloaded(__WORD[] code) => new ([0, 0, ..code], [], 0);

    public readonly int QueryStart = queryStart;
    public ReadOnlySpan<__WORD> Query => Code[QueryStart..];
}