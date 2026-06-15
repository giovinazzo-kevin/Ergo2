using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;

namespace Ergo.Compiler.Emission;

public class KnowledgeBaseBytecode(__WORD[] data) : Bytecode(data, [])
{
    public readonly OperatorLookup Operators = new();
    public readonly List<string> Imports = [];

    public bool TryResolve(Lang.Ast.Signature signature, out Signature reference)
    {
        reference = default;
        if (!ConstantsLookup.TryGetValue(signature.Functor.Value, out var c))
            return false;
        var n = signature.Arity.TryGetValue(out var a) ? a : Signature.VARIADIC;
        reference = (Signature)(c, n);
        return true;
    }

    protected override void LoadData(ref ReadOnlySpan<int> span)
    {
        base.LoadData(ref span);
        LoadOperators(ref span);
        LoadImports(ref span);
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

    protected virtual void LoadImports(ref ReadOnlySpan<__WORD> span)
    {
        if (span.Length == 0) return;
        var count = span[0]; span = span[1..];
        for (int i = 0; i < count; i++) {
            var lenInWords = span[0]; span = span[1..];
            var bytes = new byte[lenInWords * sizeof(__WORD)];
            for (int j = 0; j < lenInWords; j++) {
                bytes[j * 4 + 0] = (byte)(span[j] >> 0);
                bytes[j * 4 + 1] = (byte)(span[j] >> 8);
                bytes[j * 4 + 2] = (byte)(span[j] >> 16);
                bytes[j * 4 + 3] = (byte)(span[j] >> 24);
            }
            span = span[lenInWords..];
            Imports.Add(System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0'));
        }
    }
}
