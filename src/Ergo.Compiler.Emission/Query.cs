global using VariableMap = System.Collections.Generic.Dictionary<string, int>;
namespace Ergo.Compiler.Emission;

public record Query(QueryBytecode Bytecode, VariableMap Variables);