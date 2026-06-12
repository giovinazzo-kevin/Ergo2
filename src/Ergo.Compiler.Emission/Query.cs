global using VariableMap = System.Collections.Generic.Dictionary<string, Ergo.Compiler.Emission.__VAR>;
namespace Ergo.Compiler.Emission;

public readonly record struct __VAR(string Name, int Index);
public record Query(
    QueryBytecode Bytecode,
    VariableMap Variables,
    IReadOnlyList<Delegate> BuiltInHandlers,
    IReadOnlyDictionary<__WORD, Ergo.Compiler.Analysis.AbstractTerm> AbstractTerms,
    IReadOnlyDictionary<(object Functor, int Arity), System.Func<Lang.Ast.Term[], Lang.Ast.Term>> Reconstructors,
    KnowledgeBase? Source = null)
{
    public Query(QueryBytecode bytecode, VariableMap variables)
        : this(bytecode, variables, [], new Dictionary<__WORD, Ergo.Compiler.Analysis.AbstractTerm>(), new Dictionary<(object, int), System.Func<Lang.Ast.Term[], Lang.Ast.Term>>()) { }
}
