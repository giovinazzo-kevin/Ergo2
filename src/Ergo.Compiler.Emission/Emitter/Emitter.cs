using Ergo.Compiler.Analysis;
using System;
using static Ergo.Compiler.Emission.Ops;

namespace Ergo.Compiler.Emission;


public class Emitter
{
    public virtual KnowledgeBase Compile(CallGraph graph)
    {
        var ctx = new EmitterContext(graph.Analyzer.Operators);
        foreach (var module in graph.Modules.Values)
        {
            foreach (var pred in module.Predicates.Values)
                Predicate(ctx, pred);
        }
        return new KnowledgeBase((string)graph.Root.Value, ctx.ToArray());
    }

    protected virtual void Predicate(EmitterContext ctx, Predicate predicate)
    {
        var p = ctx.Constant(predicate.Signature.Functor.Value);
        var n = predicate.Signature.Arity;
        ctx.Label((p, n), ctx.PC);
        var clauseCtxs = new EmitterContext[predicate.Clauses.Count];
        for (var i = 0; i < predicate.Clauses.Count; i++)
        {
            clauseCtxs[i] = ctx.Scope();
            clauseCtxs[i].Emit(allocate);
            for (int j = 0; j < predicate.Clauses[i].Args.Length; j++)
                Read(ctx, predicate.Clauses[i].Args[j], j);
            foreach (var goal in predicate.Clauses[i].Goals)
            {
                for (int k = 0; k < goal.Args.Length; k++)
                    Write(ctx, goal.Args[k], k);
                Goal(ctx, goal);
            }
            clauseCtxs[i].Emit(deallocate);
        }
        for (var i = 0; i < predicate.Clauses.Count; i++)
        {
            if (i == predicate.Clauses.Count - 1)
                ctx.Emit(trust_me);
            else if (i > 0)
                ctx.Emit(retry_me_else(ctx.PC + clauseCtxs[..i].Sum(x => x.PC + 2)));
            else
                ctx.Emit(try_me_else(ctx.PC + clauseCtxs[0].PC + 2));
            ctx.EmitMany(clauseCtxs[i]);
        }
    }

    protected virtual void Goal(EmitterContext ctx, Goal g)
    {
        switch (g)
        {
            case Cut:
                break;
            case StaticGoal @static:
                var p1 = ctx.Constant(@static.Callee.Signature.Functor.Value);
                var n1 = @static.Callee.Signature.Arity;
                ctx.Emit(call((Signature)(p1, n1)));
                break;
            case DynamicGoal @dynamic:
                var p2 = ctx.Constant(@dynamic.Callee.Value);
                var n2 = @dynamic.Args.Length;
                ctx.Emit(call((Signature)(p2, n2)));
                break;
            case LateBoundGoal lateBound:
                break;
            default: throw new NotSupportedException();
        }
    }

    protected virtual void Read(EmitterContext ctx, Lang.Ast.Term t, int Ai)
    {
        switch (t)
        {
            case Lang.Ast.Complex @struct:
                var f = ctx.Constant(@struct.Functor.Value);
                var fn = (Signature)(f, @struct.Arity);
                ctx.Emit(get_structure(fn, Ai));
                break;
            case Lang.Ast.Variable @var:
                ctx.Emit(get_value(0, Ai));
                break;
            case Lang.Ast.Atom @const:
                var c = ctx.Constant(@const.Value);
                ctx.Emit(get_constant(c, Ai));
                break;
            default: throw new NotSupportedException();
        }
    }

    protected virtual void Write(EmitterContext ctx, Lang.Ast.Term t, int Ai)
    {
        switch (t)
        {
            case Lang.Ast.Complex @struct:
                var f = ctx.Constant(@struct.Functor.Value);
                var fn = (Signature)(f, @struct.Arity);
                ctx.Emit(put_structure(fn, Ai));
                break;
            case Lang.Ast.Variable @var:
                ctx.Emit(put_value(0, Ai));
                break;
            case Lang.Ast.Atom @const:
                var c = ctx.Constant(@const.Value);
                ctx.Emit(put_constant(c, Ai));
                break;
            default: throw new NotSupportedException();
        }
    }
}