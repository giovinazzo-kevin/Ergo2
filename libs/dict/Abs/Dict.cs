using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Parsing;
using Ergo.Lang.Parsing.Extensions;
using Ergo.Shared.Types;
using static Ergo.Compiler.Emission.Ops;
using Term = Ergo.Compiler.Emission.Term;
using Signature = Ergo.Compiler.Emission.Signature;
using static Ergo.Compiler.Emission.Term.__TAG;
using __WORD = int;

namespace Ergo.Libs.Dict.Abs;

public sealed class Dict(Library parent) : AbstractTerm<Ast.Dict>(parent)
{
    public override Lang.Ast.Signature Signature => WellKnown.Signature;

    public override Func<Maybe<Lang.Ast.Term>> OnParse(Parser parser)
    {
        Func<Maybe<Ast.Dict>> dict = parser.Transact([() => {
            Lang.Ast.Term f;
            if (parser.Atom().TryGetValue(out var atom))
                f = atom;
            else if (parser.Variable().TryGetValue(out var v))
                f = v;
            else
                return Maybe<Ast.Dict>.None;

            // Try empty: functor{}
            var empty = parser.Parenthesized(
                Set.WellKnown.Collection,
                () => Maybe.Some<Atom>(null!));
            if (empty.TryGetValue(out _))
                return new Ast.Dict(f, []);

            // Try kvps: functor{k1: v1, k2: v2}
            var kvps = parser.Parenthesized(
                Set.WellKnown.Collection,
                parser.ConsExpression(Operators.Conjunction));
            if (kvps.TryGetValue(out var cons))
            {
                var pairs = new System.Collections.Generic.List<BinaryExpression>();
                foreach (var item in cons.Contents)
                {
                    if (item is BinaryExpression { Operator: var op } bin && op == Operators.Module)
                        pairs.Add(bin);
                    else
                        return Maybe<Ast.Dict>.None;
                }
                return new Ast.Dict(f, pairs);
            }

            // Try singleton: functor{k: v}
            var single = parser.Parenthesized(
                Set.WellKnown.Collection,
                parser.BinaryExpressionRhs);
            if (single.TryGetValue(out var singleTerm)
                && singleTerm is BinaryExpression { Operator: var sop } sbin
                && sop == Operators.Module)
                return new Ast.Dict(f, [sbin]);

            return Maybe<Ast.Dict>.None;
        }]);

        return parser.Transact([
            dict.Cast<Ast.Dict, Lang.Ast.Term>
        ]);
    }

    public override void OnEmitGet(Emitter emitter, EmitterContext ctx, int sig, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName)
    {
        var dict = (Ast.Dict)args[Ai];
        ctx.Emit(get_abstract(sig, Ai));
        emitter.EmitUnify(ctx, dict.DictFunctor);
        ctx.Emit(unify_constant(ctx.Constant((__int)dict.Pairs.Length)));
        foreach (var pair in dict.Pairs)
        {
            emitter.EmitUnify(ctx, pair.Lhs);
            emitter.EmitUnify(ctx, pair.Rhs);
        }
    }

    public override void OnEmitPut(Emitter emitter, EmitterContext ctx, int sig, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName, bool deep)
    {
        var dict = (Ast.Dict)args[Ai];
        if (!deep) return;
        ctx.Emit(put_abstract(sig, Ai));
        emitter.EmitSet(ctx, dict.DictFunctor, varsByName);
        emitter.EmitSet(ctx, (__int)dict.Pairs.Length, varsByName);
        foreach (var pair in dict.Pairs)
        {
            emitter.EmitSet(ctx, pair.Lhs, varsByName);
            emitter.EmitSet(ctx, pair.Rhs, varsByName);
        }
    }

    public override void OnUnify(Runtime.WAM.ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo)
    {
        // Heap: [sig, functor, N, k1, v1, k2, v2, ...]
        // Unify functors
        todo.Push((addr1 + 1, addr2 + 1));

        var n1Term = (Term)vm.Heap[vm.deref(addr1 + 2)];
        var n2Term = (Term)vm.Heap[vm.deref(addr2 + 2)];
        if (n1Term.Tag != CON || n2Term.Tag != CON) { vm.fail = true; return; }
        var n1 = (int)((Lang.Ast.__int)vm.Constants[n1Term.Value]).Value;
        var n2 = (int)((Lang.Ast.__int)vm.Constants[n2Term.Value]).Value;

        int smallAddr, largeAddr, smallN, largeN;
        if (n1 <= n2) { smallAddr = addr1; largeAddr = addr2; smallN = n1; largeN = n2; }
        else { smallAddr = addr2; largeAddr = addr1; smallN = n2; largeN = n1; }

        for (int i = 0; i < smallN; i++)
        {
            var smallKeyAddr = vm.deref(smallAddr + 3 + i * 2);
            var smallValAddr = smallAddr + 3 + i * 2 + 1;
            var sk = (Term)vm.Store[smallKeyAddr];

            bool found = false;
            for (int j = 0; j < largeN; j++)
            {
                var largeKeyAddr = vm.deref(largeAddr + 3 + j * 2);
                var lk = (Term)vm.Store[largeKeyAddr];
                if (sk.Tag == CON && lk.Tag == CON && sk.Value == lk.Value)
                {
                    todo.Push((smallValAddr, largeAddr + 3 + j * 2 + 1));
                    found = true;
                    break;
                }
            }
            if (!found) { vm.fail = true; return; }
        }
    }

    public override Lang.Ast.Term OnGet(Runtime.WAM.ErgoVM vm, int addr)
    {
        var functor = vm.read_heap_term(addr + 1);
        var nTerm = (Term)vm.Heap[vm.deref(addr + 2)];
        var n = (int)((Lang.Ast.__int)vm.Constants[nTerm.Value]).Value;

        var pairs = new BinaryExpression[n];
        for (int i = 0; i < n; i++)
        {
            var key = vm.read_heap_term(addr + 3 + i * 2);
            var val = vm.read_heap_term(addr + 3 + i * 2 + 1);
            pairs[i] = new BinaryExpression(Operators.Module, key, val);
        }
        return new Ast.Dict(functor, pairs);
    }

    public override int OnPut(Runtime.WAM.ErgoVM vm, Ast.Dict dict)
    {
        var dictSigConst = vm._QUERY.Bytecode.AddConstant(new Lang.Ast.__string((string)WellKnown.Functor.Value));
        var dictSig = (Signature)(dictSigConst, 2);
        var baseAddr = vm.H;
        vm.Heap[vm.H++] = dictSig;
        vm.Heap[vm.H++] = vm.write_heap_term(dict.DictFunctor);
        var nConst = vm._QUERY.Bytecode.AddConstant((__int)dict.Pairs.Length);
        vm.Heap[vm.H++] = (Term)(CON, nConst);
        foreach (var pair in dict.Pairs)
        {
            vm.Heap[vm.H++] = vm.write_heap_term(pair.Lhs);
            vm.Heap[vm.H++] = vm.write_heap_term(pair.Rhs);
        }
        return (Term)(ABS, baseAddr);
    }

    public override string OnPretty(Runtime.WAM.ErgoVM vm, int addr, bool quoted)
    {
        var functor = vm.pretty((Term)vm.Heap[addr + 1], quoted);
        var nTerm = (Term)vm.Heap[vm.deref(addr + 2)];
        var n = (int)((Lang.Ast.__int)vm.Constants[nTerm.Value]).Value;

        var pairs = new string[n];
        for (int i = 0; i < n; i++)
        {
            var key = vm.pretty((Term)vm.Heap[addr + 3 + i * 2], quoted);
            var val = vm.pretty((Term)vm.Heap[addr + 3 + i * 2 + 1], quoted);
            pairs[i] = $"{key}: {val}";
        }
        return $"{functor}{{{string.Join(", ", pairs)}}}";
    }
}
