using Ergo.Language.Ast;
using Ergo.Language.Ast.WellKnown;
using Ergo.Shared.Types;
using System.Reflection;

namespace Ergo.Language.Lexer;

public class OperatorLookup
{
    protected readonly List<string> functors = [];
    protected readonly Dictionary<string, List<Operator>> table = [];
    protected readonly HashSet<Operator> operators = [];
    public IReadOnlyList<string> Functors => functors;
    public IEnumerable<Operator> Operators => operators;

    public OperatorLookup(IEnumerable<Operator> addOperators = null!)
    {
        AddOperators(BuiltInOperators);
        if (addOperators != null)
            AddOperators(addOperators);
    }

    public void AddOperators(params IEnumerable<Operator> ops)
    {
        foreach (var op in ops)
        {
            if (operators.Contains(op))
                continue;
            operators.Add(op);
            foreach (__string fun in op.Functors)
            {
                var funStr = (string)fun.Value;
                functors.Add(funStr);
                if (!table.TryGetValue(funStr, out var list))
                    list = table[funStr] = [];
                list.Add(op);
            }
        }
        functors.Sort((a, b) => a.Length - b.Length);
    }


    public IEnumerable<char> GetNthSymbols(int index) => functors.Select(o => o.ElementAtOrDefault(index)).Where(x => x != 0);

    public Maybe<IEnumerable<Operator>> GetOperatorsFromFunctor(string functor)
    {
        if (!table.TryGetValue(functor, out var ops))
            return default;
        if (!ops.Any())
            return default;
        return ops;
    }

    private static readonly Operator[] BuiltInOperators = typeof(Operators)
        .GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(f => f.FieldType == typeof(Operator))
        .Select(f => (Operator)f.GetValue(null)!)
        .ToArray();
}
