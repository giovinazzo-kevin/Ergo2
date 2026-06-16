using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Ergo.Shared.Types;
using EmSig = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.Reflection.BuiltIns;

/// <summary>
/// term(Functor, Args, Term) — bidirectional term decomposition/construction.
/// Decompose: if Term is a compound, unify Functor with its functor atom and Args with its argument list.
/// Construct: if Term is unbound and Functor/Args are bound, build the compound on the heap.
/// If Term is an atom, Functor unifies with it and Args unifies with [].
/// This is the primitive underlying =../2.
/// </summary>
public sealed class TermBuiltIn(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new((__string)"term", 3);
    public override ErgoVM.__op Handle => vm =>
    {
        // A[0] = Functor, A[1] = Args (list), A[2] = Term
        var termAddr = vm.deref(ErgoVM.arg_addr(2));
        var termCell = (Term)vm.Store[termAddr];

        if (termCell.Tag == Term.__TAG.STR)
        {
            // Decompose: Term is a compound structure
            var fAddr = termCell.Value;
            var sig = (EmSig)vm.Store[fAddr];
            var functorConst = sig.F;
            var arity = sig.N;

            // Unify Functor with the functor atom
            var functorCon = (Term)(Term.__TAG.CON, functorConst);
            var functorHeapAddr = vm.H;
            vm.Heap[vm.H++] = functorCon;
            vm.unify(ErgoVM.arg_addr(0), functorHeapAddr);
            if (vm.fail) return;

            // Build the args as a list on the heap and unify with Args
            var argsListWord = BuildListFromStructArgs(vm, fAddr, arity);
            var argsHeapAddr = vm.H;
            vm.Heap[vm.H++] = argsListWord;
            vm.unify(ErgoVM.arg_addr(1), argsHeapAddr);
        }
        else if (termCell.Tag == Term.__TAG.CON)
        {
            // Term is an atom: Functor = atom, Args = []
            vm.unify(ErgoVM.arg_addr(0), termAddr);
            if (vm.fail) return;

            // Unify Args with empty list
            var emptyConst = vm._QUERY.Bytecode.AddConstant(List.WellKnown.EmptyList);
            var emptyAddr = vm.H;
            vm.Heap[vm.H++] = (Term)(Term.__TAG.CON, emptyConst);
            vm.unify(ErgoVM.arg_addr(1), emptyAddr);
        }
        else if (termCell.Tag == Term.__TAG.REF)
        {
            // Construct: Term is unbound, build from Functor + Args
            var functorAddr = vm.deref(ErgoVM.arg_addr(0));
            var functorCell = (Term)vm.Store[functorAddr];
            if (functorCell.Tag != Term.__TAG.CON) { vm.fail = true; return; }

            // Walk the args list to collect addresses
            var argAddrs = new List<int>();
            var listAddr = vm.deref(ErgoVM.arg_addr(1));
            var listCell = (Term)vm.Store[listAddr];

            while (listCell.Tag == Term.__TAG.ABS)
            {
                var dataAddr = listCell.Value + 1; // skip sig
                argAddrs.Add(dataAddr); // head
                var tailAddr = vm.deref(dataAddr + 1);
                listCell = (Term)vm.Store[tailAddr];
                listAddr = tailAddr;
            }
            // listCell should now be CON pointing to "[]"

            // Build the structure on the heap
            var structAddr = vm.H;
            vm.Heap[vm.H++] = (EmSig)(functorCell.Value, argAddrs.Count);
            foreach (var argAddr in argAddrs)
                vm.Heap[vm.H++] = vm.Store[argAddr];

            // Bind Term to the new structure
            var strWord = (Term)(Term.__TAG.STR, structAddr);
            var strAddr = vm.H;
            vm.Heap[vm.H++] = strWord;
            vm.unify(termAddr, strAddr);
        }
        else
        {
            vm.fail = true;
        }
    };

    /// <summary>
    /// Builds a list on the heap from the arguments of a structure at fAddr.
    /// Returns a word (CON for empty, ABS for non-empty) that represents the list.
    /// </summary>
    private static int BuildListFromStructArgs(ErgoVM vm, int fAddr, int arity)
    {
        if (arity == 0)
        {
            var emptyConst = vm._QUERY.Bytecode.AddConstant(List.WellKnown.EmptyList);
            return (Term)(Term.__TAG.CON, emptyConst);
        }

        // Build list cons cells bottom-up (last element first)
        var listSigConst = vm._QUERY.Bytecode.AddConstant(new __string((string)List.WellKnown.Functor.Value));
        var listSig = (EmSig)(listSigConst, 2);
        var emptyC = vm._QUERY.Bytecode.AddConstant(List.WellKnown.EmptyList);
        int tail = (Term)(Term.__TAG.CON, emptyC);

        for (int i = arity - 1; i >= 0; i--)
        {
            var pairAddr = vm.H;
            vm.Heap[vm.H++] = listSig;
            vm.Heap[vm.H++] = vm.Store[fAddr + 1 + i]; // arg cell
            vm.Heap[vm.H++] = tail;
            tail = (Term)(Term.__TAG.ABS, pairAddr);
        }
        return tail;
    }
}
