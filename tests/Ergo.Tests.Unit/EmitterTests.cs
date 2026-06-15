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

    [Theory]
    [InlineData("backtrack_tests", "value(X)", new[] { "X/1", "X/2", "X/3" })]
    [InlineData("emitter_tests", "multiple_fact", new[] { "", "" })]
    public void Backtracking_ProducesAllSolutions(string module, string query, string[] expected)
        => AssertSolutions(new ErgoVM().findall(CompileQuery(Consult(module), query)), expected);
}
