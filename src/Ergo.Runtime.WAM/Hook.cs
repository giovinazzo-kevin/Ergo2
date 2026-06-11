using Ergo.Compiler.Emission;
using static Ergo.Compiler.Emission.Ops;
using Signature = Ergo.Compiler.Emission.Signature;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.Runtime.WAM;

/// <summary>
/// A well-known query that C# calls into ergo.
/// Inbound FFI. Builtins are the outbound FFI.
/// Compiles itself from a KB and a signature.
/// </summary>
public readonly struct Hook
{
    public readonly Query Query;

    public Hook(KnowledgeBase kb, __WORD sig)
    {
        var ctx = EmitterContext.From(kb.Bytecode);
        ctx.Emit(call((Signature)sig));
        ctx.Emit(halt);
        Query = new Query(ctx.ToQuery(kb.Bytecode), []);
    }

    public Lang.Ast.Term? Call(ErgoVM vm, params Lang.Ast.Term[] args)
    {
        for (int i = 0; i < args.Length; i++)
            vm.A[i] = vm.WriteHeapTerm(args[i]);

        var outAddr = vm.H;
        vm.Heap[vm.H] = (Term)(REF, vm.H);
        vm.A[args.Length] = (Term)(REF, outAddr);
        vm.H++;

        vm.Run(Query);

        if (vm.fail) return null;
        return vm.ReadHeapTerm(vm.deref(outAddr));
    }

    public bool Fire(ErgoVM vm, params Lang.Ast.Term[] args)
    {
        for (int i = 0; i < args.Length; i++)
            vm.A[i] = vm.WriteHeapTerm(args[i]);

        vm.Run(Query);
        return !vm.fail;
    }
}
