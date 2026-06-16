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

namespace Ergo.Libs.Set.Abs;

public sealed class Set(Library parent) : AbstractTerm<Ast.Set>(parent)
{
    public override Lang.Ast.Signature Signature => WellKnown.Functor / 2;

    public override Func<Maybe<Lang.Ast.Term>> OnParse(Parser parser)
    {
        Func<Maybe<Lang.Ast.Term>> emptySet = parser.Transact([() =>
            parser.Parenthesized(WellKnown.Collection, () => Maybe.Some<Atom>(null!))
                .Select<Lang.Ast.Term>(_ => WellKnown.EmptySet)
        ]);
        Func<Maybe<Ast.Set>> setHeadTail = parser.Transact([() =>
            parser.Parenthesized(WellKnown.Collection, parser.HeadTailExpression)
                .Select(x => new Ast.Set(ExtractHead(x), x.Rhs))
        ]);
        Func<Maybe<Ast.Set>> setNoTail = parser.Transact([() =>
            parser.Parenthesized(WellKnown.Collection, parser.ConsExpression(Operators.Conjunction))
                .Select(x => new Ast.Set(x.Contents))
        ]);
        Func<Maybe<Ast.Set>> setSingleton = parser.Transact([() =>
            parser.Parenthesized(WellKnown.Collection, parser.BinaryExpressionRhs)
                .Select(x => new Ast.Set([x]))
        ]);
        return parser.Transact([
            emptySet,
            setHeadTail.Cast<Ast.Set, Lang.Ast.Term>,
            setNoTail.Cast<Ast.Set, Lang.Ast.Term>,
            setSingleton.Cast<Ast.Set, Lang.Ast.Term>
        ]);
    }

    private static IEnumerable<Lang.Ast.Term> ExtractHead(BinaryExpression headTail)
    {
        if (headTail.Lhs is ConsExpression cons)
            return cons.Contents;
        if (headTail.Lhs is BinaryExpression { IsCons: true, Operator: var pOp } pseudoCons
            && pOp.Equals(Operators.Conjunction))
            return new ConsExpression(pseudoCons.Operator, pseudoCons.Lhs, pseudoCons.Rhs).Contents;
        return [headTail.Lhs];
    }

    public override void OnEmitGet(Emitter emitter, EmitterContext ctx, int sig, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName)
    {
        var set = (Ast.Set)args[Ai];
        if (set.Head.Any()) {
            ctx.Emit(get_abstract(sig, Ai));
            foreach (var elem in set.Head)
                emitter.EmitUnify(ctx, elem);
            emitter.EmitUnify(ctx, set.Tail);
        } else {
            ctx.Emit(get_constant(ctx.Constant(WellKnown.EmptySet.Value), Ai));
        }
    }

    public override void OnEmitPut(Emitter emitter, EmitterContext ctx, int sig, Lang.Ast.Term[] args, int Ai, Dictionary<string, int>? varsByName, bool deep)
    {
        var set = (Ast.Set)args[Ai];
        if (!deep) return;
        var elems = set.Head.ToArray();
        if (elems.Length == 0) {
            ctx.Emit(put_constant(ctx.Constant(WellKnown.EmptySet.Value), Ai));
            return;
        }

        int prevV = -1;
        for (int k = elems.Length - 1; k >= 0; k--) {
            int reg = k == 0 ? Ai : Ai + elems.Length - k;
            ctx.Emit(put_abstract(sig, reg));
            emitter.EmitSet(ctx, elems[k], varsByName);
            if (prevV < 0) emitter.EmitSet(ctx, set.Tail, varsByName);
            else ctx.Emit(set_value(prevV));
            if (k > 0) { prevV = ctx.NumVars++; ctx.Emit(get_variable(prevV, reg)); }
        }
    }

    public override void OnUnify(Runtime.WAM.ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo)
    {
        // Same as list for now — element-wise head + tail
        // Order-independent unification is a future optimization
        todo.Push((addr1 + 1, addr2 + 1));
        todo.Push((addr1 + 2, addr2 + 2));
    }

    public override Lang.Ast.Term OnGet(Runtime.WAM.ErgoVM vm, int addr)
    {
        var elements = new List<Lang.Ast.Term>();
        Lang.Ast.Term tail = WellKnown.EmptySet;
        var dataAddr = addr + 1;
        while (true) {
            elements.Add(vm.read_heap_term(dataAddr));
            var tailTerm = (Term)vm.Heap[dataAddr + 1];
            if (tailTerm.Tag == REF) {
                var d = vm.deref(tailTerm.Value);
                var resolved = (Term)vm.Store[d];
                if (resolved.Tag == REF && resolved.Value == d)
                    tail = new Variable($"_{d}");
                else
                    tail = vm.read_heap_term(d);
                break;
            }
            if (tailTerm.Tag == CON) {
                tail = vm.Constants[tailTerm.Value];
                break;
            }
            if (tailTerm.Tag != ABS) {
                tail = vm.read_heap_term(dataAddr + 1);
                break;
            }
            dataAddr = tailTerm.Value + 1;
        }
        return new Ast.Set(elements, tail);
    }

    public override int OnPut(Runtime.WAM.ErgoVM vm, Ast.Set set)
    {
        var elems = set.Head.ToArray();
        if (elems.Length == 0) {
            var c = vm._QUERY.Bytecode.AddConstant(WellKnown.EmptySet);
            return (int)(Term)(CON, c);
        }
        var c2 = vm._QUERY.Bytecode.AddConstant(new __string((string)WellKnown.Functor.Value));
        var setSig = (Signature)(c2, 2);
        var tail = vm.write_heap_term(set.Tail);
        for (int i = elems.Length - 1; i >= 0; i--) {
            var pairAddr = vm.H;
            vm.Heap[vm.H++] = setSig;
            vm.Heap[vm.H++] = vm.write_heap_term(elems[i]);
            vm.Heap[vm.H++] = tail;
            tail = (Term)(ABS, pairAddr);
        }
        return tail;
    }

    public override string OnPretty(Runtime.WAM.ErgoVM vm, int addr, bool quoted)
    {
        var elems = new List<string>();
        var dataAddr = addr + 1;
        while (true) {
            var head = (Term)vm.Heap[dataAddr];
            var tail = (Term)vm.Heap[dataAddr + 1];
            elems.Add(vm.pretty(head, quoted));
            if (tail.Tag == CON && vm.Constants[tail.Value].Value is string s && s == "{}")
                break;
            if (tail.Tag == ABS) {
                dataAddr = tail.Value + 1;
                continue;
            }
            if (tail.Tag == REF) {
                var d = vm.deref(tail.Value);
                var dt = (Term)vm.Store[d];
                if (dt.Tag == ABS) { dataAddr = dt.Value + 1; continue; }
                if (dt.Tag == CON && vm.Constants[dt.Value].Value is string s2 && s2 == "{}") break;
                elems.Add("|" + vm.pretty(dt, quoted));
                break;
            }
            elems.Add("|" + vm.pretty(tail, quoted));
            break;
        }
        return $"{{{string.Join(",", elems)}}}";
    }
}
