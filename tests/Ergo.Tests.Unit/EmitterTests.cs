using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;
using System.Diagnostics;

namespace Ergo.UnitTests;

public class EmitterTests : Tests
{
    private const string MODULE = "emitter_tests";

    [Fact]
    public void vm_tests()
    {
        var module = nameof(vm_tests);
        Emit(module, out var graph, out var kb);
        foreach (var p in graph.Modules[module].Imports.Single(x => x.Name == "stdlib").Predicates.Keys)
            Trace.WriteLine(p.ToString());
        var sig = new Lang.Ast.Signature("parse", 1);
        Assert.True(kb.Bytecode.TryResolve(sig, out var entry), "Missing parse/1");
        var span = kb.Bytecode.Code[kb.Bytecode.Labels[entry]..];
    }

    [Fact]
    public void emitter_tests()
    {
        var module = nameof(emitter_tests);
        Emit(module, out var graph, out var kb);
        var query = CompileQuery(kb, "fact").Bytecode;
        var span = query.Code;
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("fact", 0, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(13, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("fact", 0, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.trust_me, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("another_fact", 0, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst(0, ref span, query);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst(1, ref span, query);
        AssertInt32(1, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst(2, ref span, query);
        AssertInt32(2, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(38, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("john", ref span, query);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("mary", ref span, query);
        AssertInt32(1, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.trust_me, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("mary", ref span, query);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("susan", ref span, query);
        AssertInt32(1, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(69, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.get_variable, ref span);
        AssertInt32(0, ref span);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_variable, ref span);
        AssertInt32(1, ref span);
        AssertInt32(1, ref span);
        AssertOp(OpCode.get_level, ref span);
        AssertInt32(2, ref span);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(0, ref span);
        AssertInt32(0, ref span);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(1, ref span);
        AssertInt32(1, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("parent", 2, ref span, query);
        AssertOp(OpCode.cut, ref span);
        AssertInt32(2, ref span);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.trust_me, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.get_variable, ref span);
        AssertInt32(0, ref span);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_variable, ref span);
        AssertInt32(1, ref span);
        AssertInt32(1, ref span);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(0, ref span);
        AssertInt32(0, ref span);
        AssertOp(OpCode.put_variable, ref span);
        AssertInt32(2, ref span);
        AssertInt32(1, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("parent", 2, ref span, query);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(2, ref span);
        AssertInt32(0, ref span);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(1, ref span);
        AssertInt32(1, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("ancestor", 2, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
    }

    [Fact]
    public void TryMeElse_Should_Allocate_ChoicePoint()
    {
        var vm = new ErgoVM();
        vm._QUERY = QueryBytecode.EMPTY;
        vm.E = ErgoVM.HEAP_SIZE;
        vm.B = ErgoVM.HEAP_SIZE;
        vm.N = 2;
        vm.A[0] = 111;
        vm.A[1] = 222;
        vm.TryMeElse();

        var b = vm.B;
        Assert.Equal(2, vm.Store[b]);
        Assert.Equal(111, vm.Store[b + 1]);
        Assert.Equal(222, vm.Store[b + 2]);
        Assert.Equal(ErgoVM.HEAP_SIZE, vm.Store[b + 3]);
    }

    [Fact]
    public void RetryMeElse_Should_Reset_ChoicePoint_And_Advance_Label()
    {
        var vm = new ErgoVM();
        vm._QUERY = QueryBytecode.Preloaded([999]);
        vm.N = 2;
        vm.A[0] = 111;
        vm.A[1] = 222;
        vm.E = ErgoVM.HEAP_SIZE;
        vm.B = ErgoVM.HEAP_SIZE + 10;
        vm.Store[vm.B] = 2;
        vm.CP = 1234;
        vm.TR = 44;
        vm.H = 77;
        vm.HB = 77;
        vm.B0 = ErgoVM.HEAP_SIZE;

        vm.TryMeElse();

        var b = vm.B;
        vm.TR = 55;
        vm.H = 88;
        vm.P = 0;

        vm.RetryMeElse();

        Assert.Equal(ErgoVM.HEAP_SIZE, vm.E);
        Assert.Equal(ErgoVM.HEAP_SIZE + 20, vm.B);
        Assert.Equal(2, vm.Store[b]);
        Assert.Equal(111, vm.Store[b + 1]);
        Assert.Equal(222, vm.Store[b + 2]);
        Assert.Equal(1234, vm.CP);
        Assert.Equal(44, vm.TR);
        Assert.Equal(77, vm.H);
        Assert.Equal(77, vm.HB);
        Assert.Equal(1, vm.P);
    }
}


