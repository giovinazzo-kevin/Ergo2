using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using System.Text;

namespace Ergo.UnitTests;

public class Tests
{
    public delegate void Validator(QueryBytecode bytes);

    protected KnowledgeBase Consult(string moduleName)
    {
        const string MODULE_PATH = "./ergo/";
        const string BIN_PATH = "./bin/";
        var kbLocator = new KnowledgeBaseLocator(BIN_PATH);
        var compiledKb = kbLocator.Index.Find(moduleName).FirstOrDefault();
        if (compiledKb != null)
            return new KnowledgeBase(ErgoFileStream.Open(compiledKb));
        var moduleLocator = new ModuleLocator(MODULE_PATH);
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        var graph = analyzer.LoadModule(moduleName);
        var emitter = new Emitter();
        var kb = emitter.KnowledgeBase(graph);
        return kb;
    }

    protected void AssertQuery(string query, Validator validate)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var q = kb.Query(query);
        validate(q.Bytecode);
    }

    protected void Emit(string moduleName, out CallGraph graph, out KnowledgeBase kb)
    {
        var moduleLocator = new ModuleLocator("./ergo/");
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        graph = analyzer.LoadModule(moduleName);
        var emitter = new Emitter();
        kb = emitter.KnowledgeBase(graph);
    }

    protected void AssertOp(OpCode op, ref ReadOnlySpan<int> span)
    {
        Assert.Equal(op, (OpCode)span[0]);
        span = span[1..];
    }

    protected void AssertInt32(int value, ref ReadOnlySpan<int> span)
    {
        Assert.Equal(value, span[0]);
        span = span[1..];
    }


    protected void AssertSignature(string f, int n, ref ReadOnlySpan<int> span, Bytecode b)
    {
        var sign = (Signature)span[0];
        Assert.Equal(f, b.Constants[sign.F].Value);
        Assert.Equal(n, sign.N);
        span = span[1..];
    }

    protected void AssertConst(object value, ref ReadOnlySpan<int> span, Bytecode b)
    {
        Assert.Equal(value, b.Constants[span[0]].Value);
        span = span[1..];
    }

    protected void AssertUTF8(string value, ref ReadOnlySpan<int> span)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        AssertInt32(bytes.Length, ref span);
        for (int i = 0; i < bytes.Length; i++)
        {
            Assert.Equal(bytes[i], span[0]);
            span = span[1..];
        }
    }


}
