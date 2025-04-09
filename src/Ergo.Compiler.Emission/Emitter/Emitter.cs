using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;
using System;
using System.Linq;
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
        var ctx = EmitterContext.From(kb);
        var scope = JITGoal(ctx.Scope(), query, out var needsStackFrame);
        var variableMap = new VariableMap();
        var vars = query.GetVariables().Distinct().ToArray();
        for (int i = 0; i < vars.Length; i++)
        {
            vars[i].Value = (__int)i;
            variableMap[vars[i].Name] = new(vars[i].Name, i);
        }
        if (needsStackFrame)
            ctx.Emit(allocate);
        ctx.Concat(scope);
        if (needsStackFrame)
            ctx.Emit(deallocate);
        var code = ctx.ToQuery(kb);
        return new Query(code, variableMap);

        EmitterContext JITGoal(EmitterContext ctx, Lang.Ast.Term term, out bool needsStackFrame)
        {
            if (term is __string { Value: "!" })
            {
                var cutReg = ctx.NumVars++;
                ctx.Emit(get_level(cutReg));
                ctx.Emit(cut(cutReg));
                needsStackFrame = true;
                return ctx;
            }
            if (term is BinaryExpression { IsCons: true } cons && cons.Operator == Operators.Conjunction)
            {
                JITGoal(ctx, cons.Lhs, out var lhsNeedsStackFrame);
                JITGoal(ctx, cons.Rhs, out var rhsNeedsStackFrame);
                needsStackFrame = lhsNeedsStackFrame || rhsNeedsStackFrame;
                return ctx;
            }
            var sign = term.GetSignature().GetOrThrow(); // TODO: THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER
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

            var args = clause.Args.ToArray();
            for (int j = 0; j < args.Length; j++)
                Read(clauseCtxs[i], args, j); // assign inside Read!

            clauseCtxs[i].NumVars = args.OfType<Variable>().Select(v => (int)(__int)v.Value).DefaultIfEmpty(-1).Max() + 1;

            int? cutReg = null;
            if (clause.Goals.Any(g => g is Cut))
            {
                cutReg = clauseCtxs[i].NumVars++;
                clauseCtxs[i].Emit(get_level((int)cutReg));
            }

            foreach (var goal in clause.Goals)
            {
                for (int k = 0; k < goal.Args.Length; k++)
                    Write(clauseCtxs[i], goal.Args, k);
                Goal(clauseCtxs[i], goal, cutReg);
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


    protected virtual void Goal(EmitterContext ctx, Goal g, int? cutReg = null)
    {
        switch (g)
        {
            case Cut:
                if (cutReg is not int reg)
                    throw new InvalidOperationException("Cut emitted without get_level.");
                ctx.Emit(cut(reg));
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

    protected virtual void EmitUnify(EmitterContext ctx, Lang.Ast.Term term)
    {
        switch (term)
        {
            case Variable v when v.Value is not __int:
                v.Value = (__int)ctx.NumVars;
                ctx.Emit(unify_variable(ctx.NumVars++));
                break;

            case Variable v:
                ctx.Emit(unify_value((__WORD)(__int)v.Value));
                break;

            case Atom c:
                var constId = ctx.Constant(c.Value);
                ctx.Emit(unify_constant(constId));
                break;

            default:
                throw new NotSupportedException("Nested compound terms not yet supported inside structure heads.");
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
                foreach (var arg in @struct.Args)
                    EmitUnify(ctx, arg);

                break;
            case Variable @var when var.Value is not __int:
                var.Value = (__int)ctx.NumVars;
                ctx.Emit(get_variable(ctx.NumVars++, Ai));
                break;
            case Variable @var:
                ctx.Emit(get_value((__WORD)(__int)var.Value, Ai));
                break;
            case Atom @const:
                var c = ctx.Constant(@const.Value);
                ctx.Emit(get_constant(c, Ai));
                break;
            default:
                throw new NotSupportedException();
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