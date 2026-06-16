using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Extensions;
using static Ergo.Compiler.Emission.Ops;

namespace Ergo.Compiler.Emission;


public class Emitter
{
    private readonly Dictionary<Lang.Ast.Signature, AbstractTerm> _abstractTerms = new();

    public void RegisterAbstractTermEmitter(AbstractTerm abs, EmitterContext ctx)
    {
        abs.PackedSig = (int)(Signature)(ctx.Constant(abs.Signature.Functor.Value), abs.Signature.Arity.GetOrThrow());
        _abstractTerms[abs.Signature] = abs;
    }
    public virtual KnowledgeBase KnowledgeBase(CallGraph graph)
    {
        var ctx = new EmitterContext(graph.Analyzer.Operators);
        var builtIns = new List<BuiltIn>();
        var abstractTerms = new List<AbstractTerm>();
        foreach (var module in graph.Modules.Values) {
            foreach (var import in module.Imports)
                ctx.AddImport(import.Name);
            // Collect and register abstract terms BEFORE predicate emission
            foreach (var lib in module.Libraries)
                abstractTerms.AddRange(lib.ExportedAbstractTerms);
        }
        foreach (var abs in abstractTerms)
            RegisterAbstractTermEmitter(abs, ctx);
        foreach (var module in graph.Modules.Values) {
            foreach (var pred in module.Predicates.Values) {
                if (pred.BuiltIns.Count > 0 && pred.Clauses.Count == 0)
                    builtIns.AddRange(pred.BuiltIns);
                else
                    Predicate(ctx, pred);
            }
        }
        var code = ctx.ToKnowledgeBase();
        var kb = new KnowledgeBase((string)graph.Root.Value, code);
        foreach (var bi in builtIns)
            kb.RegisterBuiltInLabel((string)bi.Signature.Functor.Value, bi.Signature.Arity, bi.Handler);
        foreach (var abs in abstractTerms)
        {
            kb.RegisterAbstractTerm(abs);
            RegisterAbstractTermEmitter(abs, ctx);
        }
#if EMITTER_TRACE
        System.Diagnostics.Trace.WriteLine(ctx.Dump(query: false));
#endif
        return kb;
    }

    public virtual Query Query(Lang.Ast.Term query, KnowledgeBaseBytecode kb)
    {
        var ctx = EmitterContext.From(kb);
        var queryVars = new Dictionary<string, int>();
        var scope = JITGoal(ctx.Scope(), query, out var needsStackFrame, queryVars);
        var variableMap = new VariableMap();
        var queryArgs = query.Args;
        var vars = query.Variables.Distinct().ToArray();
        for (int i = 0; i < vars.Length; i++) {
            vars[i].Value = (__int)i;
            // Find the actual A register index for this variable
            int ai = i; // fallback for conjunctions
            for (int j = 0; j < queryArgs.Length; j++) {
                if (queryArgs[j] is Variable v && v.Name == vars[i].Name) {
                    ai = j;
                    break;
                }
            }
            variableMap[vars[i].Name] = new(vars[i].Name, ai);
        }
        ctx.Emit(allocate);
        ctx.Concat(scope);
        // Restore query variable bindings from stack frame to A after call returns.
        // All query vars were pre-allocated as permanent via put_variable in JITGoal,
        // so put_unsafe_value correctly reads from Store[E + Yn + 2] for all of them.
        foreach (var (name, vIdx) in queryVars) {
            if (variableMap.TryGetValue(name, out var entry))
                ctx.Emit(put_unsafe_value(vIdx, entry.Index));
        }
        ctx.Emit(deallocate);
        ctx.Emit(proceed);
        var code = ctx.ToQuery(kb);
#if EMITTER_TRACE
        System.Diagnostics.Trace.WriteLine(ctx.Dump(query: true));
#endif
        return new Query(code, variableMap);

        EmitterContext JITGoal(EmitterContext ctx, Lang.Ast.Term term, out bool needsStackFrame, Dictionary<string, int> queryVars)
        {
            if (term is __string { Value: "!" }) {
                var cutReg = ctx.NumVars++;
                ctx.Emit(get_level(cutReg));
                ctx.Emit(cut(cutReg));
                needsStackFrame = true;
                return ctx;
            }
            if (term is BinaryExpression { IsCons: true } cons && cons.Operator == Operators.Conjunction) {
                JITGoal(ctx, cons.Lhs, out var lhsNeedsStackFrame, queryVars);
                JITGoal(ctx, cons.Rhs, out var rhsNeedsStackFrame, queryVars);
                needsStackFrame = lhsNeedsStackFrame || rhsNeedsStackFrame;
                return ctx;
            }
            var sign = term.GetSignature().GetOrThrow(); // TODO: THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER
            // Unwrap module qualification: io:write(X) ? write(X)
            var goal = term is BinaryExpression { Operator: var op } bin && op == Operators.Module
                ? bin.Rhs : term;
            needsStackFrame = goal.Variables.Any();
            var args = goal.Args;

            // Pre-allocate query variables that appear ONLY inside nested compounds.
            // Direct arg variables get put_variable naturally (writes to stack frame).
            // Nested variables would get set_variable (heap only, no stack frame),
            // so we pre-allocate them here with put_variable to scratch A registers.
            var directArgVarNames = new HashSet<string>();
            for (int i = 0; i < args.Length; i++)
                if (args[i] is Variable dv) directArgVarNames.Add(dv.Name);
            var scratchA = args.Length;
            foreach (var qv in goal.Variables.Distinct()) {
                if (!directArgVarNames.Contains(qv.Name) && !queryVars.ContainsKey(qv.Name)) {
                    var newIdx = ctx.NumVars;
                    queryVars[qv.Name] = newIdx;
                    qv.Value = (__int)newIdx;
                    ctx.Emit(put_variable(ctx.NumVars++, scratchA++));
                }
            }

            for (int i = 0; i < args.Length; ++i) {
                if (args[i] is Variable v && queryVars.TryGetValue(v.Name, out var knownIdx))
                    ctx.Emit(put_value(knownIdx, i));
                else if (args[i] is Variable v2) {
                    // Should not reach here -- all vars pre-allocated above
                    var newIdx = ctx.NumVars;
                    queryVars[v2.Name] = newIdx;
                    ctx.Emit(put_variable(ctx.NumVars++, i));
                } else
                    Write(ctx, args, i, queryVars, deep: true);
            }
            if (!kb.TryResolve(sign, out var label)) {
                // Check if it's declared dynamic � emit call for runtime resolution
                var c = ctx.Constant(sign.Functor.Value);
                var dynSig = (Signature)(c, sign.Arity.TryGetValue(out var dynA) ? dynA : Signature.VARIADIC);
                if (kb.Labels.ContainsKey(dynSig))
                    label = dynSig;
                else
                    throw new InvalidOperationException($"Predicate {sign} could not be resolved");
            }
            ctx.Emit(call(label));
            return ctx;
        }
    }

    protected virtual void Predicate(EmitterContext ctx, Predicate predicate)
    {
        var p = ctx.Constant(predicate.Signature.Functor.Value);
        var n = predicate.Signature.Arity.TryGetValue(out var predArity) ? predArity : Signature.VARIADIC;
        ctx.Label((p, n), ctx.PC);
        var clauseCtxs = new EmitterContext[predicate.Clauses.Count];

        foreach (var (clause, i) in predicate.Clauses.Iterate()) {
            clauseCtxs[i] = ctx.Scope();

            if (clause.NeedsStackFrame)
                clauseCtxs[i].Emit(allocate);

            var args = clause.Args.ToArray();
            for (int j = 0; j < args.Length; j++)
                Read(clauseCtxs[i], args, j); // assign inside Read!

            clauseCtxs[i].NumVars = args.OfType<Variable>().Select(v => (int)(__int)v.Value).DefaultIfEmpty(-1).Max() + 1;

            int? cutReg = null;
            if (clause.Goals.Any(g => g is Cut)) {
                cutReg = clauseCtxs[i].NumVars++;
                clauseCtxs[i].Emit(get_level((int)cutReg));
            }

            foreach (var goal in clause.Goals) {
                for (int k = 0; k < goal.Args.Length; k++)
                    Write(clauseCtxs[i], goal.Args, k);
                Goal(clauseCtxs[i], goal, cutReg);
            }

            if (clause.NeedsStackFrame)
                clauseCtxs[i].Emit(deallocate);
            clauseCtxs[i].Emit(proceed);
        }

        for (var i = 0; i < predicate.Clauses.Count; i++) {
            if (predicate.Clauses.Count > 1) {
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
        switch (g) {
            case Cut:
                if (cutReg is not int reg) {
                    reg = ctx.NumVars++;
                    ctx.Emit(get_level(reg));
                    cutReg = reg;
                }
                ctx.Emit(cut((int)cutReg));
                break;
            case Fail:
                ctx.Emit(Ops.fail);
                break;
            case StaticGoal @static:
                var p1 = ctx.Constant(@static.Callee.Signature.Functor.Value);
                var n1 = @static.Callee.Signature.Arity.TryGetValue(out var sA) ? sA : Signature.VARIADIC;
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

    public virtual void EmitUnify(EmitterContext ctx, Lang.Ast.Term term)
    {
        switch (term) {
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
                throw new NotSupportedException($"Unsupported term type in unify: {term.GetType().Name}");
        }
    }

    protected virtual void Read(EmitterContext ctx, Lang.Ast.Term[] args, int Ai)
    {
        Read(ctx, args, Ai, null);
    }

    protected virtual void Read(EmitterContext ctx, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName)
    {
        switch (args[Ai]) {
            case var _ when args[Ai].GetSignature().TryGetValue(out var sig) && _abstractTerms.TryGetValue(sig, out var match):
                ((WellKnown.Delegates.EmitGet)match.EmitGet)(this, ctx, match.PackedSig, args, Ai, varsByName);
                break;
            case Complex @struct:
                var f = ctx.Constant(@struct.Functor.Value);
                var fn = (Signature)(f, @struct.Arity);
                ctx.Emit(get_structure(fn, Ai));
                foreach (var arg in @struct.Args)
                    EmitUnify(ctx, arg);

                break;
            case Variable @var when var.Value is not __int:
                var idx = ctx.NumVars;
                var.Value = (__int)idx;
                if (varsByName != null) varsByName[@var.Name] = idx;
                ctx.Emit(get_variable(ctx.NumVars++, Ai));
                break;
            case Variable @var when varsByName != null && varsByName.TryGetValue(@var.Name, out var knownIdx):
                ctx.Emit(get_value(knownIdx, Ai));
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
        Write(ctx, args, Ai, null);
    }

    protected virtual void Write(EmitterContext ctx, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName, bool deep = false)
    {
        switch (args[Ai]) {
            case var _ when args[Ai].GetSignature().TryGetValue(out var sig) && _abstractTerms.TryGetValue(sig, out var match):
                ((WellKnown.Delegates.EmitPut)match.EmitPut)(this, ctx, match.PackedSig, args, Ai, varsByName, deep);
                break;
            case Complex @struct:
                var f = ctx.Constant(@struct.Functor.Value);
                var fn = (Signature)(f, @struct.Arity);
                if (deep) {
                    // Build nested sub-args bottom-up in temp registers
                    int nextTempA = Ai + 1;
                    var subVRegs = new int[@struct.Args.Length];
                    var isNested = new bool[@struct.Args.Length];
                    for (int k = 0; k < @struct.Args.Length; k++) {
                        if (@struct.Args[k] is Complex nested) {
                            subVRegs[k] = WriteDeepComplex(ctx, nested, ref nextTempA, varsByName);
                            isNested[k] = true;
                        }
                    }
                    ctx.Emit(put_structure(fn, Ai));
                    for (int k = 0; k < @struct.Args.Length; k++) {
                        if (isNested[k])
                            ctx.Emit(set_value(subVRegs[k]));
                        else
                            EmitSet(ctx, @struct.Args[k], varsByName);
                    }
                } else {
                    ctx.Emit(put_structure(fn, Ai));
                }
                break;
            case Variable @var when var.Value is __int @i:
                ctx.Emit(put_value((__WORD)@i, Ai));
                break;
            case Variable @var when varsByName != null && varsByName.TryGetValue(@var.Name, out var knownIdx):
                ctx.Emit(put_value(knownIdx, Ai));
                break;
            case Variable @var:
                var newIdx = ctx.NumVars;
                var.Value = (__int)newIdx;
                if (varsByName != null) varsByName[@var.Name] = newIdx;
                ctx.Emit(put_variable(ctx.NumVars++, Ai));
                break;
            case Atom @const:
                var c = ctx.Constant(@const.Value);
                ctx.Emit(put_constant(c, Ai));
                break;
            default: throw new NotSupportedException();
        }
    }

    public virtual void EmitSet(EmitterContext ctx, Lang.Ast.Term term, Dictionary<string, int>? varsByName = null)
    {
        switch (term) {
            case Variable v when v.Value is __int i:
                System.Diagnostics.Trace.WriteLine($"[EMIT] set_value (via Value) for '{v.Name}' ? V[{(__WORD)i}]");
                ctx.Emit(set_value((__WORD)i));
                break;
            case Variable v when varsByName != null && varsByName.TryGetValue(v.Name, out var knownIdx):
                System.Diagnostics.Trace.WriteLine($"[EMIT] set_value for '{v.Name}' ? V[{knownIdx}]");
                ctx.Emit(set_value(knownIdx));
                break;
            case Variable v:
                System.Diagnostics.Trace.WriteLine($"[EMIT] set_variable for '{v.Name}' (varsByName={varsByName != null}, found={varsByName?.ContainsKey(v.Name)})");
                var newIdx = ctx.NumVars;
                v.Value = (__int)newIdx;
                if (varsByName != null) varsByName[v.Name] = newIdx;
                ctx.Emit(set_variable(ctx.NumVars++));
                break;
            case Atom c:
                ctx.Emit(set_constant(ctx.Constant(c.Value)));
                break;
            default:
                throw new NotSupportedException($"Nested compound terms in set mode not yet supported: {term.GetType().Name}");
        }
    }

    /// <summary>
    /// Recursively builds a compound term on the heap bottom-up.
    /// Inner structures are built first in temp A registers, copied to V via get_variable.
    /// Returns the V register index holding the STR reference.
    /// </summary>
    private int WriteDeepComplex(EmitterContext ctx, Complex @struct, ref int nextTempA, Dictionary<string, int>? varsByName)
    {
        // Recursively build nested Complex sub-args first
        var subVRegs = new int[@struct.Args.Length];
        var isNested = new bool[@struct.Args.Length];
        for (int k = 0; k < @struct.Args.Length; k++) {
            if (@struct.Args[k] is Complex nested) {
                subVRegs[k] = WriteDeepComplex(ctx, nested, ref nextTempA, varsByName);
                isNested[k] = true;
            }
        }

        // Build this structure
        var f = ctx.Constant(@struct.Functor.Value);
        var fn = (Signature)(f, @struct.Arity);
        var tempA = nextTempA++;
        ctx.Emit(put_structure(fn, tempA));

        for (int k = 0; k < @struct.Args.Length; k++) {
            if (isNested[k])
                ctx.Emit(set_value(subVRegs[k]));
            else
                EmitSet(ctx, @struct.Args[k], varsByName);
        }

        // Copy A[tempA] ? V[vn] for parent's set_value
        var vn = ctx.NumVars++;
        ctx.Emit(get_variable(vn, tempA));
        return vn;
    }

    /// <summary>
    /// Compiles a single dynamically asserted clause (fact or rule) into raw bytecode.
    /// Uses name-based variable tracking since read_heap_term creates distinct Variable objects.
    /// Returns the raw instruction words ready to be appended to the code buffer.
    /// </summary>
    public __WORD[] EmitDynamicClause(EmitterContext ctx, Lang.Ast.Term term)
    {
        var clause = term as Lang.Ast.Clause;
        var head = clause?.Functor ?? term;
        var goals = clause?.Goals
            .Where(g => g is not __bool { Value: true })
            .ToArray() ?? Array.Empty<Lang.Ast.Term>();
        var headArgs = head.Args;

        var scope = ctx.Scope();
        var varsByName = new Dictionary<string, int>();
        bool needsStack = goals.Length > 0;

        if (needsStack)
            scope.Emit(Ops.allocate);

        for (int j = 0; j < headArgs.Length; j++)
            Read(scope, headArgs, j, varsByName);

        scope.NumVars = varsByName.Count > 0 ? varsByName.Values.Max() + 1 : 0;

        int? cutReg = null;
        if (goals.Any(g => g is Atom a && a.Value is string s && s == "!")) {
            cutReg = scope.NumVars++;
            scope.Emit(Ops.get_level((int)cutReg));
        }

        EmitGoals(scope, goals, varsByName, cutReg);

        void EmitGoals(EmitterContext sc, IEnumerable<Lang.Ast.Term> gs, Dictionary<string, int> vars, int? cr)
        {
            foreach (var goal in gs) {
                // Flatten conjunctions into sequential goal calls
                if (goal is BinaryExpression { IsCons: true } cons
                    && cons.Operator.Equals(Lang.Ast.WellKnown.Operators.Conjunction)) {
                    var flat = new ConsExpression(cons.Operator, cons.Lhs, cons.Rhs).Contents;
                    EmitGoals(sc, flat, vars, cr);
                    continue;
                }
                if (goal is Atom a && a.Value is string s && s == "!") {
                    sc.Emit(Ops.cut((int)cr!));
                    continue;
                }
                if (goal is __bool { Value: true })
                    continue;
                if (goal is __bool { Value: false }) {
                    sc.Emit(Ops.fail);
                    continue;
                }
                var goalArgs = goal.Args;
                for (int k = 0; k < goalArgs.Length; k++)
                    Write(sc, goalArgs, k, vars, deep: true);
                var sig = goal.GetSignature().GetOrThrow();
                var p = sc.Constant(sig.Functor.Value);
                sc.Emit(Ops.call((Signature)(p, sig.Arity.TryGetValue(out var gA) ? gA : Signature.VARIADIC)));
            }
        }

        if (needsStack)
            scope.Emit(Ops.deallocate);
        scope.Emit(Ops.proceed);

        return scope.ToRawInstructions();
    }
}
