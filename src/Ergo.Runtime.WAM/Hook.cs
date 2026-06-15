using Ergo.Compiler.Emission;
using static Ergo.Compiler.Emission.Ops;
using Query = Ergo.Compiler.Emission.Query;
using Signature = Ergo.Compiler.Emission.Signature;

namespace Ergo.Runtime.WAM;

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
        vm.open_query(Query);

        for (int i = 0; i < args.Length; i++)
            vm.A[i] = vm.write_heap_term(args[i]);

        var outAddr = vm.H;
        vm.Heap[vm.H] = (Term)(Term.__TAG.REF, vm.H);
        vm.A[args.Length] = (Term)(Term.__TAG.REF, outAddr);
        vm.H++;

        if (!vm.next_solution()) {
            vm.close_query();
            return null;
        }

        var result = vm.read_heap_term(vm.deref(outAddr));
        vm.close_query();
        return result;
    }

    public bool Fire(ErgoVM vm, params Lang.Ast.Term[] args)
    {
        vm.open_query(Query);

        for (int i = 0; i < args.Length; i++)
            vm.A[i] = vm.write_heap_term(args[i]);

        var result = vm.next_solution();
        vm.close_query();
        return result;
    }
}
