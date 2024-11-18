using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Libs.Prologue;
using Ergo.Shared.Extensions;

namespace Ergo.UnitTests;

public class EmitterTests
{
    protected Emitter Emit(string moduleName, out ReadOnlySpan<byte> bytes)
    {
        var moduleLocator = new ModuleLocator("./ergo/");
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        var graph = analyzer.Load(moduleName);
        var emitter = new Emitter(graph);
        var ops = emitter.Compile();
        bytes = emitter.Emit(ops, out int sz);
        Assert.Equal(sz, bytes.Length);
        Assert.Equal(emitter.PC, bytes.Length);
        return emitter;
    }

    protected void AssertOp(Op.Type op, ref ReadOnlySpan<byte> span)
    {
        AssertByte((byte)op, ref span);
    }

    protected void AssertByte(byte value, ref ReadOnlySpan<byte> span)
    {
        Assert.Equal(value, span[0]);
        span = span[1..];
    }

    protected void AssertInt32(int value, ref ReadOnlySpan<byte> span)
    {
        var bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < bytes.Length; i++)
            Assert.Equal(bytes[i], span[i]);
        span = span[bytes.Length..];
    }

    [Fact]
    public void emitter_tests()
    {
        var module = nameof(emitter_tests);
        using var emitter = Emit(module, out var bytes);

        var fact_0 = emitter.Graph.Modules[module].Predicates[new("fact", 0)];
        var label_fact_0 = emitter.GetLabel(fact_0);
        Assert.Equal(0, label_fact_0);
        AssertOp(Op.Type.proceed, ref bytes);

        var another_fact_0 = emitter.Graph.Modules[module].Predicates[new("another_fact", 0)];
        var label_another_fact_0 = emitter.GetLabel(another_fact_0);
        Assert.Equal(1, label_another_fact_0);
        AssertOp(Op.Type.proceed, ref bytes);

        var complex_fact_3 = emitter.Graph.Modules[module].Predicates[new("complex_fact", 3)];
        var label_complex_fact_3 = emitter.GetLabel(complex_fact_3);
        Assert.Equal(2, label_complex_fact_3);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertInt32(0, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertInt32(1, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertInt32(2, ref bytes);
        AssertByte(2, ref bytes);
        AssertOp(Op.Type.proceed, ref bytes);

        var parent_2 = emitter.Graph.Modules[module].Predicates[new("parent", 2)];
        var label_parent_2 = emitter.GetLabel(parent_2);
        Assert.Equal(21, label_parent_2);
    }

}
