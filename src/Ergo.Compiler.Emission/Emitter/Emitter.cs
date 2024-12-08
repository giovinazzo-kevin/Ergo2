using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;
using System;
using static Ergo.Compiler.Emission.Ops;

namespace Ergo.Compiler.Emission;


public class Emitter
{
    public virtual KnowledgeBase KnowledgeBase(CallGraph graph)
    {
        var ctx = new EmitterContext(graph.Analyzer.Operators);
        foreach (var module in graph.Modules.Values)
        {
            foreach (var pred in module.Predicates.Values)
                Predicate(ctx, pred);
        }
        var code = ctx.ToKnowledgeBase();
        return new KnowledgeBase((string)graph.Root.Value, code);
    }

    public virtual Query Query(Lang.Ast.Term query, KnowledgeBaseBytecode kb)
    {
        var ctx = new EmitterContext(new());
        var scope = JITGoal(ctx.Scope(), query, out var needsStackFrame);
        if (needsStackFrame)
            ctx.Emit(allocate);
        ctx.Concat(scope);
        if (needsStackFrame)
            ctx.Emit(deallocate);
        var code = ctx.ToQuery(kb);
        return new Query(code);

        EmitterContext JITGoal(EmitterContext ctx, Lang.Ast.Term term, out bool needsStackFrame)
        {
            if (term is BinaryExpression { IsCons: true } cons && cons.Operator == Operators.Conjunction)
            {
                JITGoal(ctx, cons.Lhs, out var lhsNeedsStackFrame);
                JITGoal(ctx, cons.Rhs, out var rhsNeedsStackFrame);
                needsStackFrame = lhsNeedsStackFrame || rhsNeedsStackFrame;
                return ctx;
            }
            var sign = term.GetSignature()
                .GetOrThrow(); // TODO: Insufficient data for a meaningful answer
            needsStackFrame = term.GetVariables().Any();
            var args = term.GetArguments();
            for (int i = 0; i < args.Length; ++i)
                Write(ctx, args, i);
            if (!kb.TryResolve(sign, out var label))
                throw new InvalidOperationException();
            ctx.Emit(call(label));
            return ctx;
        }
    }

    protected virtual void Predicate(EmitterContext ctx, Predicate predicate)
    {
        var p = ctx.Constant(predicate.Signature.Functor.Value);
        var n = predicate.Signature.Arity;
        ctx.Label((p, n), ctx.PC);
        var clauseCtxs = new EmitterContext[predicate.Clauses.Count];
        foreach (var (clause, i) in predicate.Clauses.Iterate())
        {
            clauseCtxs[i] = ctx.Scope();
            if (clause.NeedsStackFrame)
                clauseCtxs[i].Emit(allocate);
            for (int j = 0; j < clause.Args.Length; j++)
                Read(clauseCtxs[i], clause.Args, j);
            foreach (var goal in clause.Goals)
            {
                for (int k = 0; k < goal.Args.Length; k++)
                    Write(clauseCtxs[i], goal.Args, k);
                Goal(clauseCtxs[i], goal);
            }
            if (clause.NeedsStackFrame)
                clauseCtxs[i].Emit(deallocate);
            clauseCtxs[i].Emit(proceed);
        }
        for (var i = 0; i < predicate.Clauses.Count; i++)
        {
            if (predicate.Clauses.Count > 1)
            {
                if (i == predicate.Clauses.Count - 1)
                    ctx.Emit(trust_me);
                else if (i > 0)
                    ctx.Emit(retry_me_else(ctx.PC + clauseCtxs[..i].Sum(x => x.PC + 2)));
                else
                    ctx.Emit(try_me_else(ctx.PC + clauseCtxs[0].PC + 2));
            }
            ctx.Concat(clauseCtxs[i]);
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

    protected virtual void Read(EmitterContext ctx, Lang.Ast.Term[] args, int Ai)
    {
        switch (args[Ai])
        {
            case Complex @struct:
                var f = ctx.Constant(@struct.Functor.Value);
                var fn = (Signature)(f, @struct.Arity);
                ctx.Emit(get_structure(fn, Ai));
                break;
            case Variable @var when var.Value is __int @i:
                ctx.Emit(get_value((__WORD)@i, Ai));
                break;
            case Variable @var:
                var.Value = (__int)ctx.NumVars;
                ctx.Emit(get_variable(ctx.NumVars++, Ai));
                break;
            case Atom @const:
                var c = ctx.Constant(@const.Value);
                ctx.Emit(get_constant(c, Ai));
                break;
            default: throw new NotSupportedException();
        }
    }

    protected virtual void Write(EmitterContext ctx, Lang.Ast.Term[] args, int Ai)
    {
        switch (args[Ai])
        {
            case Complex @struct:
                var f = ctx.Constant(@struct.Functor.Value);
                var fn = (Signature)(f, @struct.Arity);
                ctx.Emit(put_structure(fn, Ai));
                break;
            case Variable @var when var.Value is __int @i:
                ctx.Emit(put_value((__WORD)@i, Ai));
                break;
            case Variable @var:
                var.Value = (__int)ctx.NumVars;
                ctx.Emit(put_variable(ctx.NumVars++, Ai));
                break;
            case Atom @const:
                var c = ctx.Constant(@const.Value);
                ctx.Emit(put_constant(c, Ai));
                break;
            default: throw new NotSupportedException();
        }
    }
}