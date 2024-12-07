using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Runtime.WAM;
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

    [Fact]
    public void emitter_tests()
    {
        var module = nameof(emitter_tests);
        Emit(module, out var graph, out var kb);
        var span = kb.Bytecode.Code;
        // fact/0
        AssertOp(OpCode.proceed, ref span);
        // another_fact/0
        AssertOp(OpCode.allocate, ref span);
        AssertOp(OpCode.call, ref span);
        AssertSignature("fact", 0, ref span, kb.Bytecode);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
        // complex_fact/3
        AssertOp(OpCode.get_constant, ref span);
        AssertConst(0, ref span, kb.Bytecode);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst(1, ref span, kb.Bytecode);
        AssertInt32(1, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst(2, ref span, kb.Bytecode);
        AssertInt32(2, ref span);
        AssertOp(OpCode.proceed, ref span);
        // parent/2
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(9, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("john", ref span, kb.Bytecode);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("mary", ref span, kb.Bytecode);
        AssertInt32(1, ref span);
        AssertOp(OpCode.proceed, ref span);
        AssertOp(OpCode.trust_me, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("mary", ref span, kb.Bytecode);
        AssertInt32(0, ref span);
        AssertOp(OpCode.get_constant, ref span);
        AssertConst("susan", ref span, kb.Bytecode);
        AssertInt32(1, ref span);
        AssertOp(OpCode.proceed, ref span);
        // ancestor(X,Y) :- parent(X,Y).
        AssertOp(OpCode.try_me_else, ref span);
        AssertInt32(22, ref span);
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
        AssertSignature("parent", 2, ref span, kb.Bytecode);
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
        AssertSignature("parent", 2, ref span, kb.Bytecode);
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(2, ref span); // Z
        AssertInt32(0, ref span); // A0
        AssertOp(OpCode.put_value, ref span);
        AssertInt32(1, ref span); // Y
        AssertInt32(1, ref span); // A1
        AssertOp(OpCode.call, ref span); 
        AssertSignature("ancestor", 2, ref span, kb.Bytecode);
        AssertOp(OpCode.deallocate, ref span);
        AssertOp(OpCode.proceed, ref span);
    }

}
