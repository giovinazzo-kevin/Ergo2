using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using EmSig = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;
using Signature = Ergo.Lang.Ast.Signature;
using Ergo.Shared.Types;

namespace Ergo.Libs.Prologue.BuiltIns;

public sealed class Call(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new((__string)"call", default(Maybe<int>));
    public override ErgoVM.__op Handle => vm =>
    {
        // A[0] = the goal term, A[1..N-1] = extra args
        var extraArgs = vm.N - 1;
        var addr = vm.deref(ErgoVM.arg_addr(0));
        var cell = (Term)vm.Store[addr];

        if (cell.Tag == Term.__TAG.STR)
        {
            var fAddr = cell.Value;
            var sig = (EmSig)vm.Heap[fAddr];
            var goalArity = sig.N;
            var totalArity = goalArity + extraArgs;

            // Copy goal args into A[0..goalArity-1]
            for (int i = 0; i < goalArity; i++)
                vm.A[i] = vm.Store[fAddr + 1 + i];

            // Copy extra args into A[goalArity..totalArity-1]
            for (int i = 0; i < extraArgs; i++)
                vm.A[goalArity + i] = vm.Store[ErgoVM.arg_addr(1 + i)];

            // Resolve the new signature
            var targetSig = (EmSig)(sig.F, totalArity);
            if (vm._QUERY.Bytecode.Labels.TryGetValue(targetSig, out var target))
            {
                if (target < 0)
                {
                    // Builtin dispatch
                    var idx = -(target + 1);
                    vm.N = totalArity;
                    vm.B0 = vm.B;
                    ((ErgoVM.__op)vm._QUERY.Source.BuiltInHandlers[idx])(vm);
                }
                else
                {
                    vm.N = totalArity;
                    vm.B0 = vm.B;
                    vm.P = target;
                }
            }
            else
                vm.fail = true;
        }
        else if (cell.Tag == Term.__TAG.CON)
        {
            // Atom call: call(foo) with extra args → foo(A1, A2, ...)
            var constIdx = cell.Value;
            var totalArity = extraArgs;

            // Shift extra args into A[0..extraArgs-1]
            for (int i = 0; i < extraArgs; i++)
                vm.A[i] = vm.Store[ErgoVM.arg_addr(1 + i)];

            var targetSig = (EmSig)(constIdx, totalArity);
            if (vm._QUERY.Bytecode.Labels.TryGetValue(targetSig, out var target))
            {
                if (target < 0)
                {
                    var idx = -(target + 1);
                    vm.N = totalArity;
                    vm.B0 = vm.B;
                    ((ErgoVM.__op)vm._QUERY.Source.BuiltInHandlers[idx])(vm);
                }
                else
                {
                    vm.N = totalArity;
                    vm.B0 = vm.B;
                    vm.P = target;
                }
            }
            else
                vm.fail = true;
        }
        else
            vm.fail = true;
    };
}
