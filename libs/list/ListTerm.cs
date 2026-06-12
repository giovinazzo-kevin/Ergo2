using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Runtime.WAM;
using Term = Ergo.Compiler.Emission.Term;
using static Ergo.Compiler.Emission.Term.__TAG;

namespace Ergo.Libs.List;

public sealed class ListTerm(Library parent) : Ergo.Libs.AbstractTerm<Lang.Ast.List>(parent)
{
    public override Signature Signature => Functors.List / 2;

    public override void OnUnify(ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo)
    {
        todo.Push((addr1 + 1, addr2 + 1));
        todo.Push((addr1 + 2, addr2 + 2));
    }

    public override Lang.Ast.Term OnRead(ErgoVM vm, int addr)
    {
        var elements = new System.Collections.Generic.List<Lang.Ast.Term>();
        Lang.Ast.Term tail = Collections.List.EmptyElement;
        var dataAddr = addr + 1;
        while (true) {
            elements.Add(vm.ReadHeapTerm(dataAddr));

            var tailTerm = (Term)vm.Heap[dataAddr + 1];

            if (tailTerm.Tag == REF) {
                var d = vm.deref(tailTerm.Value);
                var resolved = (Term)vm.Store[d];
                if (resolved.Tag == REF && resolved.Value == d)
                    tail = new Variable($"_{d}");
                else
                    tail = vm.ReadHeapTerm(d);
                break;
            }

            if (tailTerm.Tag == CON) {
                tail = vm.Constants[tailTerm.Value];
                break;
            }

            if (tailTerm.Tag != ABS) {
                tail = vm.ReadHeapTerm(dataAddr + 1);
                break;
            }

            dataAddr = tailTerm.Value + 1;
        }

        return new Lang.Ast.List(elements, tail);
    }

    public override int OnWriteHeap(ErgoVM vm, Lang.Ast.Term term)
    {
        return vm.WriteHeapTerm(term);
    }

    public override string OnPretty(ErgoVM vm, int addr, bool quoted)
    {
        var elems = new System.Collections.Generic.List<string>();
        var dataAddr = addr + 1;
        while (true) {
            var head = (Term)vm.Heap[dataAddr];
            var tail = (Term)vm.Heap[dataAddr + 1];
            elems.Add(vm.Pretty(head, quoted));
            if (tail.Tag == CON && vm.Constants[tail.Value].Value is string s && s == "[]")
                break;
            if (tail.Tag == ABS) {
                dataAddr = tail.Value + 1;
                continue;
            }
            if (tail.Tag == REF) {
                var d = vm.deref(tail.Value);
                var dt = (Term)vm.Store[d];
                if (dt.Tag == ABS) { dataAddr = dt.Value + 1; continue; }
                if (dt.Tag == CON && vm.Constants[dt.Value].Value is string s2 && s2 == "[]") break;
                elems.Add("|" + vm.Pretty(dt, quoted));
                break;
            }
            elems.Add("|" + vm.Pretty(tail, quoted));
            break;
        }
        return $"[{string.Join(",", elems)}]";
    }
}
