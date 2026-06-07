using Ergo.Compiler.Emission;
using Ergo.Compiler.Analysis;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Pipelines;
using Ergo.Pipelines.Compiler;
using Ergo.Runtime.WAM;
using System.Text;
using Query = Ergo.Compiler.Emission.Query;
using Signature = Ergo.Compiler.Emission.Signature;

namespace Ergo.UnitTests;

public class Tests
{
    public delegate void Validator(QueryBytecode bytes);

    protected KnowledgeBase Consult(string moduleName)
    {
        return Pipeline.Consult
            .Or(Pipeline.Compile)
            .Run((__string)moduleName)
            .GetOrThrow();
    }

    protected Query AssertQuery(string file, string query, Validator validate)
    {
        var kb = Consult(file);
        var q = kb.Query(query);
        validate(q.Bytecode);
        return q;
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

    protected void AssertSolutions(List<ErgoVM.Solution> solutions, string[] expected)
    {
        var actual = solutions.Select(s => s.ToString()).ToArray();
        Assert.Equal(expected, actual);
    }

}
