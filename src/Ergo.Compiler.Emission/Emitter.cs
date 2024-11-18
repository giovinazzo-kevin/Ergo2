using Ergo.Compiler.Analysis;
using System.Diagnostics;

namespace Ergo.Compiler.Emission;
using static Op;

public class Emitter(CallGraph graph) : IDisposable
{
    public int PC { get; private set; } = 0;
    public readonly CallGraph Graph = graph;
    protected readonly Dictionary<string, int> Labels = [];

    #region Helpers
    public int GetLabel(Predicate pred) => Labels[PredicateLabel(pred)];
    protected static string PredicateLabel(Predicate predicate)
        => predicate.Signature.Expl;
    public int GetLabel(Clause pred) => Labels[ClauseLabel(pred)];
    protected static string ClauseLabel(Clause clause)
        => clause.Parent.Signature.Expl + "_" + (clause.Parent.Clauses.IndexOf(clause) + 1);
    #endregion
    public ReadOnlySpan<Op> Compile()
    {
        return Inner().ToArray().AsSpan();
        IEnumerable<Op> Inner()
        {
            // Compile predicates
            foreach (var module in Graph.Modules.Values)
            {
                foreach (var op in module.Predicates.Values.SelectMany(Emit))
                    yield return op;
            }
        }
    }
    public ReadOnlySpan<byte> Emit(ReadOnlySpan<Op> ops, out int sz)
    {
        sz = 0;
        for (int i = 0; i < ops.Length; i++)
            sz += ops[i].Size;
        var bytes = new byte[sz];
        var span = bytes.AsSpan();
        foreach (var op in ops)
            Debug.Assert(op.Size == op.Emit(ref span));
        Debug.Assert(span.Length == 0);
        return bytes;
    }
    public ReadOnlySpan<byte> Emit(out int sz)
    {
        var ops = Compile().ToArray().AsSpan();
        return Emit(ops, out sz);
    }
    protected IEnumerable<Op> Emit(Predicate predicate)
    {
        Labels[PredicateLabel(predicate)] = PC;
        foreach (var op in predicate.Clauses.SelectMany(Emit))
            yield return op;
    }
    protected IEnumerable<Op> Emit(Clause clause)
    {
        Labels[ClauseLabel(clause)] = PC;
        for (byte i = 0; i < clause.Args.Length; i++)
            yield return clause.Args[i] switch {
                Lang.Ast.Atom c => op(get_constant(c, i)),
                _ => op(noop),
            };

        foreach (var op in clause.Goals.SelectMany(Emit))
            yield return op;
        yield return op(proceed);
    }
    protected IEnumerable<Op> Emit(Goal goal)
    {
        return goal switch {
            StaticGoal sg => StaticGoal(sg),
            _ => []
        };

        IEnumerable<Op> StaticGoal(StaticGoal goal)
        {
            if (goal.Callee.Signature.Arity > byte.MaxValue)
                throw new InvalidOperationException();
            var P = goal.Callee.Signature.Functor.Expl;
            var N = (byte)goal.Callee.Signature.Arity;
            yield return op(call(P, N));
        }
    }
    protected Op op(Op op)
    {
        PC += op.Size;
        return op;
    }
    protected IEnumerable<Op> ops(params IEnumerable<Op> ops)
    {
        foreach (var x in ops)
            yield return op(x);
    }
    public void Dispose()
    {

    }
}