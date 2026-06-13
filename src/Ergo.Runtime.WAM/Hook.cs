using Ergo.Compiler.Emission;
using static Ergo.Compiler.Emission.Ops;
using Query = Ergo.Compiler.Emission.Query;
using Signature = Ergo.Compiler.Emission.Signature;

namespace Ergo.Runtime.WAM;

/// <summary>
/// A well-known query that C# calls into ergo.
/// Inbound FFI. Builtins are the outbound FFI.
/// </summary>
public readonly struct Hook
{
    public readonly Query Query;

    public Hook(KnowledgeBase kb, __WORD sig)
    {
        var ctx = EmitterContext.From(kb.Bytecode);
        ctx.Emit(call((Signature)sig));
        Query = new Query(ctx.ToQuery(kb.Bytecode), [], kb);
    }

    public Lang.Ast.Term? Call(ErgoVM vm, params Lang.Ast.Term[] args)
    {
        vm.KB = Query.Source;
        vm._QUERY = Query.Bytecode;

        for (int i = 0; i < args.Length; i++)
            vm.A[i] = vm.WriteHeapTerm(args[i]);

        var outAddr = vm.H;
        vm.Heap[vm.H] = (Term)(Term.__TAG.REF, vm.H);
        vm.A[args.Length] = (Term)(Term.__TAG.REF, outAddr);
        vm.H++;

        vm.Run(Query);

        if (vm.fail && vm.exit)
            return null;

        return vm.ReadHeapTerm(vm.deref(outAddr));
    }

    public bool Fire(ErgoVM vm, params Lang.Ast.Term[] args)
    {
        vm.KB = Query.Source;
        vm._QUERY = Query.Bytecode;

        for (int i = 0; i < args.Length; i++)
            vm.A[i] = vm.WriteHeapTerm(args[i]);

        vm.Run(Query);

        return !(vm.fail && vm.exit);
    }
}
