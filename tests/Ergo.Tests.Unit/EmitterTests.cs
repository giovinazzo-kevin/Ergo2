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
        graph = analyzer.LoadModule(moduleName);
        var emitter = new Emitter();
        kb = emitter.Compile(graph);
    }

    protected void AssertOp(OpCode op, ref ReadOnlySpan<byte> span)
    {
        Assert.Equal(op, (OpCode)span[0]);
        span = span[1..];
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
    }

}
