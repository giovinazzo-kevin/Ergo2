using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;

namespace Ergo.Compiler.Emission;

public class KnowledgeBaseBytecode(__WORD[] data) : Bytecode(data, [])
{
    public readonly OperatorLookup Operators = new();

    public bool TryResolve(Lang.Ast.Signature signature, out Signature reference)
    {
        reference = default;
        if (!ConstantsLookup.TryGetValue(signature.Functor.Value, out var c))
            return false;
        reference = (Signature)(c, signature.Arity);
        return true;
    }

    protected override void LoadData(ref ReadOnlySpan<int> span)
    {
        base.LoadData(ref span);
        LoadOperators(ref span);
    }

    protected virtual void LoadOperators(ref ReadOnlySpan<int> span)
    {
        var numOfOperators = span[0]; span = span[1..];
        var ops = new List<Operator>();
        for (int i = 0; i < numOfOperators; i++)
            ops.Add(DeserializeOperator(ref span));
        Operators.AddRange(ops);
    }

    protected virtual Operator DeserializeOperator(ref ReadOnlySpan<__WORD> span)
    {
        var numOfFunctors = span[0];
        var precedence = span[1];
        var type = (Operator.Type)span[2];
        span = span[3..];
        var functors = new Atom[numOfFunctors];
        for (int i = 0; i < numOfFunctors; i++)
            functors[i] = _consts[span[i]];
        span = span[numOfFunctors..];
        return new Operator(precedence, type, functors);
    }
}
