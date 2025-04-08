global using VariableMap = System.Collections.Generic.Dictionary<string, Ergo.Compiler.Emission.__VAR>;
namespace Ergo.Compiler.Emission;

public readonly record struct __VAR(string Name, int Index);
public record Query(QueryBytecode Bytecode, VariableMap Variables);