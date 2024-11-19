using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using System.Text;

namespace Ergo.UnitTests;

public class EmitterTests
{
    protected void Emit(string moduleName, out CallGraph graph, out KnowledgeBase kb)
    {
        var moduleLocator = new ModuleLocator("./ergo/");
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        graph = analyzer.Load(moduleName);
        kb = Emitter.KnowledgeBase(graph);
    }

    protected void AssertOp(Op.Type op, ref ReadOnlySpan<byte> span)
    {
        Assert.Equal(op, (Op.Type)span[0]);
        span = span[1..];
    }

    protected void AssertRuntimeType(RuntimeType.Type type, ref ReadOnlySpan<byte> span)
    {
        AssertByte((byte)type, ref span);
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

    protected void AssertUTF8(string value, ref ReadOnlySpan<byte> span)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        AssertInt32(bytes.Length, ref span);
        for (int i = 0; i < bytes.Length; i++)
        {
            Assert.Equal(bytes[i], span[0]);
            span = span[1..];
        }
    }

    [Fact]
    public void emitter_tests()
    {
        var module = nameof(emitter_tests);
        Emit(module, out var graph, out var kb);
        var bytes = kb.Memory.Span;
        // fact/0
        var fact_0 = graph.Modules[module].Predicates[new("fact", 0)];
        var label_fact_0 = kb.GetLabel(fact_0);
        Assert.Equal(0, label_fact_0);

        AssertOp(Op.Type.proceed, ref bytes);

        // another_fact/0
        var another_fact_0 = graph.Modules[module].Predicates[new("another_fact", 0)];
        var label_another_fact_0 = kb.GetLabel(another_fact_0);
        Assert.Equal(1, label_another_fact_0);

        AssertOp(Op.Type.proceed, ref bytes);

        // complex_fact/3
        var complex_fact_3 = graph.Modules[module].Predicates[new("complex_fact", 3)];
        var label_complex_fact_3 = kb.GetLabel(complex_fact_3);
        Assert.Equal(2, label_complex_fact_3);

        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__int, ref bytes);
        AssertInt32(0, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__int, ref bytes);
        AssertInt32(1, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__int, ref bytes);
        AssertInt32(2, ref bytes);
        AssertByte(2, ref bytes);
        AssertOp(Op.Type.proceed, ref bytes);

        // parent/2
        var parent_2 = graph.Modules[module].Predicates[new("parent", 2)];
        var label_parent_2 = kb.GetLabel(parent_2);
        Assert.Equal(24, label_parent_2);

        var label_parent_2_1 = kb.GetLabel(parent_2.Clauses[0]);
        Assert.Equal(24, label_parent_2_1);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__string, ref bytes);
        AssertUTF8("john", ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__string, ref bytes);
        AssertUTF8("mary", ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.proceed, ref bytes);

        var label_parent_2_2 = kb.GetLabel(parent_2.Clauses[1]);
        Assert.Equal(47, label_parent_2_2);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__string, ref bytes);
        AssertUTF8("mary", ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.get_constant, ref bytes);
        AssertRuntimeType(RuntimeType.Type.__string, ref bytes);
        AssertUTF8("susan", ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.proceed, ref bytes);

        // ancestor/2
        var ancestor_2 = graph.Modules[module].Predicates[new("ancestor", 2)];
        var label_ancestor_2 = kb.GetLabel(ancestor_2);
        Assert.Equal(71, label_ancestor_2);

        var label_ancestor_2_1 = kb.GetLabel(ancestor_2.Clauses[0]);
        Assert.Equal(71, label_ancestor_2_1);
        AssertOp(Op.Type.get_variable, ref bytes);
        AssertByte(0, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.get_variable, ref bytes);
        AssertByte(1, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.put_value, ref bytes);
        AssertByte(0, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.put_value, ref bytes);
        AssertByte(1, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.call, ref bytes);
        AssertUTF8("parent", ref bytes);
        AssertByte(2, ref bytes);
        AssertOp(Op.Type.proceed, ref bytes);

        var label_ancestor_2_2 = kb.GetLabel(ancestor_2.Clauses[1]);
        Assert.Equal(96, label_ancestor_2_2);
        AssertOp(Op.Type.allocate, ref bytes);
        AssertOp(Op.Type.get_variable, ref bytes);
        AssertByte(0, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.get_variable, ref bytes);
        AssertByte(1, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.put_value, ref bytes);
        AssertByte(0, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.put_variable, ref bytes);
        AssertByte(2, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.call, ref bytes);
        AssertUTF8("parent", ref bytes);
        AssertByte(2, ref bytes);
        AssertOp(Op.Type.put_value, ref bytes);
        AssertByte(2, ref bytes);
        AssertByte(0, ref bytes);
        AssertOp(Op.Type.put_value, ref bytes);
        AssertByte(1, ref bytes);
        AssertByte(1, ref bytes);
        AssertOp(Op.Type.call, ref bytes);
        AssertUTF8("ancestor", ref bytes);
        AssertByte(2, ref bytes);
        AssertOp(Op.Type.deallocate, ref bytes);
        AssertOp(Op.Type.proceed, ref bytes);
    }

}
