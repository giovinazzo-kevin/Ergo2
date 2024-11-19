using Ergo.Compiler.Analysis;
using System.Diagnostics;

namespace Ergo.Compiler.Emission;
using static Op;

public static class Emitter
{
    public static KnowledgeBase KnowledgeBase(CallGraph graph)
    {
        var kb = new KnowledgeBase(graph.RootModule);
        kb.Operators.AddRange(graph.Analyzer.Operators.Values);
        var ops = 
            graph.Modules.Values
            .SelectMany(module => module
                .Predicates.Values
                    .SelectMany(pred => Emit(pred, kb)))
            .ToArray();
        var sz = 0;
        for (int i = 0; i < ops.Length; i++)
            sz += ops[i].Size;
        var mem = new byte[sz];
        kb.Memory = mem;
        var span = mem.AsSpan();
        foreach (var op in ops)
            if (op.Size != op.Emit(ref span))
                Debug.Assert(false);
        Debug.Assert(span.Length == 0);
        return kb;
    }
    public static Query Query(KnowledgeBase kb, Clause toplevel)
    {
        var query = new Query();
        var locals = new Dictionary<Lang.Ast.Variable, byte>();
        var ops = toplevel.Goals
            .SelectMany(g => Emit(g, kb, locals))
            .ToArray();
        var sz = 0;
        for (int i = 0; i < ops.Length; i++)
            sz += ops[i].Size;
        var mem = new byte[sz + 1];
        mem[^1] = (byte)halt.Type_;
<<<<<<< HEAD
        query.Program = mem;
=======
        query.Memory = mem;
>>>>>>> e815e388bd85b6597a5fcb0cfa240c268b1249ee
        var span = mem.AsSpan();
        foreach (var op in ops)
            if (op.Size != op.Emit(ref span))
                Debug.Assert(false);
        Debug.Assert(span.Length == 1);
        return query;
    }
    static IEnumerable<Op> Emit(Predicate predicate, KnowledgeBase kb)
    {
        kb.SetLabel(predicate);
        foreach (var op in predicate.Clauses.SelectMany(c => Emit(c, kb)))
            yield return op;
    }
    static IEnumerable<Op> Emit(Clause clause, KnowledgeBase kb)
    {
        var locals = new Dictionary<Lang.Ast.Variable, byte>();
        kb.SetLabel(clause);
        if (clause.IsRecursive)
            yield return op(allocate, kb);
        for (byte i = 0; i < clause.Args.Length; i++)
            yield return get(clause.Args[i], i, kb);
        foreach (var op in clause.Goals.SelectMany(g => Emit(g, kb, locals)))
            yield return op;
        if (clause.IsRecursive)
            yield return op(deallocate, kb);
        yield return op(proceed, kb);
    }
    static IEnumerable<Op> Emit(Goal goal, KnowledgeBase kb, Dictionary<Lang.Ast.Variable, byte> locals)
    {
        return goal switch {
            StaticGoal sg => StaticGoal(sg),
            _ => []
        };

        IEnumerable<Op> StaticGoal(StaticGoal goal)
        {
            if (goal.Args.Length > byte.MaxValue)
                throw new InvalidOperationException();
            if (goal.Callee.Signature.Arity > byte.MaxValue)
                throw new InvalidOperationException();

            var L = goal.Args.Length;
            var V = 0;
            for (byte Ai = 0; Ai < L; ++Ai)
            {
                yield return goal.Args[Ai] switch
                {
                    Lang.Ast.Variable v when goal.Parent.TryGetVariable(v, out var Xn)
                        => op(put_value(Xn, Ai), kb),
                    Lang.Ast.Variable v when locals.TryGetValue(v, out var Xn)
                        => op(put_value(Xn, Ai), kb),
                    Lang.Ast.Variable v when (locals[v] = (byte)(L + V++)) <= byte.MaxValue
                        => op(put_variable(locals[v], Ai), kb),
                    Lang.Ast.Atom a
                        => op(put_constant(a, Ai), kb),
                    _ => op(put_value(Ai, Ai), kb)
                };
            }

            var P = goal.Callee.Signature.Functor.Expl;
            var N = (byte)goal.Callee.Signature.Arity;
            yield return op(call(P, N), kb);
        }
    }
    static Op op(Op op, KnowledgeBase kb)
    {
        kb.PC += op.Size;
        return op;
    }
    static Op get(Lang.Ast.Term term, byte i, KnowledgeBase kb)
    {
        return term switch
        {
            Lang.Ast.Atom c => op(get_constant(c, i), kb),
            Lang.Ast.Variable v => op(get_variable(i, i), kb),
            _ => throw new NotSupportedException(),
        };
    }
}