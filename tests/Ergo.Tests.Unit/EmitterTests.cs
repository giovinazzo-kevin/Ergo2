using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class EmitterTests : Tests
{
    [Fact]
    public void emitter_tests()
    {
        var module = nameof(emitter_tests);
        Emit(module, out var graph, out var kb);
        var query = kb.Query("fact").Bytecode;
        var span = query.Code;
        // fact/0
        AssertOp(OpCode.proceed, ref span);
        // another_fact/0
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("fact", 0, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        // multiple_fact/0 (1)
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(13, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("fact", 0, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        // multiple_fact/0 (2)
        AssertOp(OpCode.trust_me, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("another_fact", 0, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        // complex_fact/3
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
        // parent/2
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
        // ancestor(X,Y) :- parent(X,Y).
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(65, ref span);
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
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(1, ref span);
        AssertInt32(1, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("parent", 2, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        // ancestor(X,Y) :- parent(X,Z), ancestor(Z,Y).
        AssertOp(OpCode.trust_me, ref span);
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.get_variable, ref span);
        AssertInt32(0, ref span); // X
        AssertInt32(0, ref span); // A0
        AssertOp(OpCode.get_variable, ref span);
        AssertInt32(1, ref span); // Y
        AssertInt32(1, ref span); // A1
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(0, ref span); // X
        AssertInt32(0, ref span); // A0
        AssertOp(OpCode.put_variable, ref span);
        AssertInt32(2, ref span); // Z
        AssertInt32(1, ref span); // A1
        AssertOp(OpCode.call, ref span); 
        AssertSignature("parent", 2, ref span, query);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(2, ref span); // Z
        AssertInt32(0, ref span); // A0
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(1, ref span); // Y
        AssertInt32(1, ref span); // A1
        AssertOp(OpCode.call, ref span); 
        AssertSignature("ancestor", 2, ref span, query);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        // ?- fact.
        AssertOp(OpCode.call, ref span);
        AssertSignature("fact", 0, ref span, query);
    }

    [Fact]
    public void TryMeElse_Should_Allocate_ChoicePoint()
    {
        var vm = new ErgoVM();
        vm._QUERY = QueryBytecode.EMPTY;
        vm.N = 2;
        vm.A[0] = 111;
        vm.A[1] = 222;
        vm.TryMeElse(); // fake __addr() in test

        var b = vm.B;
        Assert.Equal(2, vm.Stack[b]);         // N
        Assert.Equal(111, vm.Stack[b + 1]);   // A[0]
        Assert.Equal(222, vm.Stack[b + 2]);   // A[1]
        Assert.Equal(vm.E, vm.Stack[b + 3]);  // E
    }

    [Fact]
    public void RetryMeElse_Should_Reset_ChoicePoint_And_Advance_Label()
    {
        var vm = new ErgoVM();
        vm._QUERY = QueryBytecode.Preloaded([999]);
        vm.N = 2;
        vm.A[0] = 111;
        vm.A[1] = 222;
        vm.E = 0;
        vm.B = 10;
        vm.Stack[vm.B] = 2; // N
        vm.CP = 1234;
        vm.TR = 44;
        vm.H = 77;
        vm.HB = 77;
        vm.B0 = 0;

        // TryMeElse sets up the choice point
        vm.TryMeElse();

        var b = vm.B;
        vm.TR = 55;  // Move TR forward to simulate trail growth
        vm.H = 88;   // Same for H

        // Provide a dummy address
        vm.P = 0;

        vm.RetryMeElse();

        Assert.Equal(0, vm.E);
        Assert.Equal(20, vm.B);
        Assert.Equal(2, vm.Stack[b]);       // N
        Assert.Equal(111, vm.Stack[b + 1]); // A[0]
        Assert.Equal(222, vm.Stack[b + 2]); // A[1]
        Assert.Equal(1234, vm.CP);
        Assert.Equal(44, vm.TR); // Rolled back
        Assert.Equal(77, vm.H);  // Rolled back
        Assert.Equal(77, vm.HB); // Reset
        Assert.Equal(1, vm.P);
    }

}
