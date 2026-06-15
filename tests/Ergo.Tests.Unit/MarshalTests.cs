using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Libs.List.Ast;
using Ergo.Runtime.WAM;
using Term = Ergo.Compiler.Emission.Term;

namespace Ergo.UnitTests;

public class MarshalTests : Tests
{
    private const string MODULE = "list_tests";

    private ErgoVM SetupVM()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var q = CompileQuery(kb, "fact");
        vm.open_query(q);
        vm.next_solution();
        return vm;
    }

    private Lang.Ast.Term Roundtrip(ErgoVM vm, Lang.Ast.Term term)
    {
        var word = vm.WriteHeapTerm(term);
        var addr = vm.H;
        vm.Heap[vm.H++] = word;
        return vm.ReadHeapTerm(addr);
    }

    #region WriteHeapTerm
    [Theory]
    [InlineData("hello")]
    [InlineData("world")]
    [InlineData("")]
    [InlineData("foo_bar")]
    [InlineData("a")]
    public void WriteHeapTerm_String(string value)
    {
        var vm = SetupVM();
        var term = (Term)vm.WriteHeapTerm((__string)value);
        Assert.Equal(Term.__TAG.CON, term.Tag);
        Assert.Equal(value, vm._QUERY.Bytecode.Constants[term.Value].Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void WriteHeapTerm_Int(int value)
    {
        var vm = SetupVM();
        var term = (Term)vm.WriteHeapTerm((__int)value);
        Assert.Equal(Term.__TAG.CON, term.Tag);
        Assert.Equal(value, vm._QUERY.Bytecode.Constants[term.Value].Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WriteHeapTerm_Bool(bool value)
    {
        var vm = SetupVM();
        var term = (Term)vm.WriteHeapTerm((__bool)value);
        Assert.Equal(Term.__TAG.CON, term.Tag);
        Assert.Equal(value, vm._QUERY.Bytecode.Constants[term.Value].Value);
    }

    [Theory]
    [InlineData(3.14)]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    public void WriteHeapTerm_Double(double value)
    {
        var vm = SetupVM();
        var term = (Term)vm.WriteHeapTerm((__double)value);
        Assert.Equal(Term.__TAG.CON, term.Tag);
        Assert.Equal(value, vm._QUERY.Bytecode.Constants[term.Value].Value);
    }

    [Theory]
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "world", false)]
    public void WriteHeapTerm_ConstantDedup(string a, string b, bool same)
    {
        var vm = SetupVM();
        var wa = (Term)vm.WriteHeapTerm((__string)a);
        var wb = (Term)vm.WriteHeapTerm((__string)b);
        Assert.Equal(same, wa.Value == wb.Value);
    }
    #endregion

    #region Roundtrip — Atoms
    [Theory]
    [InlineData("hello")]
    [InlineData("foo")]
    [InlineData("bar_baz")]
    [InlineData("")]
    public void Roundtrip_Atom(string value)
    {
        var vm = SetupVM();
        var read = Roundtrip(vm, (__string)value);
        Assert.IsType<__string>(read);
        Assert.Equal(value, ((Atom)read).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-1)]
    public void Roundtrip_Int(int value)
    {
        var vm = SetupVM();
        var read = Roundtrip(vm, (__int)value);
        Assert.Equal(value, ((Atom)read).Value);
    }
    #endregion

    #region Roundtrip — Complex
    [Theory]
    [InlineData("f", "a")]
    [InlineData("parent", "john", "mary")]
    [InlineData("rgb", "r", "g", "b")]
    [InlineData("big", "a", "b", "c", "d", "e")]
    public void Roundtrip_Complex(string functor, params string[] args)
    {
        var vm = SetupVM();
        var astArgs = args.Select(a => (Lang.Ast.Term)(__string)a).ToArray();
        var read = Roundtrip(vm, new Complex((__string)functor, astArgs));
        Assert.IsType<Complex>(read);
        var c = (Complex)read;
        Assert.Equal(functor, (string)c.Functor.Value);
        Assert.Equal(args.Length, c.Args.Length);
        for (int i = 0; i < args.Length; i++)
            Assert.Equal(args[i], (string)((Atom)c.Args[i]).Value);
    }
    #endregion

    #region Roundtrip — Lists
    [Theory]
    [InlineData("a", "b", "c")]
    [InlineData("x")]
    [InlineData("1", "2", "3", "4", "5")]
    public void Roundtrip_List(params string[] elems)
    {
        var vm = SetupVM();
        var astElems = elems.Select(e => (Lang.Ast.Term)(__string)e).ToArray();
        var read = Roundtrip(vm, new List(astElems, Ergo.Libs.List.WellKnown.EmptyList));
        Assert.IsType<List>(read);
        var head = ((List)read).Head.ToArray();
        Assert.Equal(elems.Length, head.Length);
        for (int i = 0; i < elems.Length; i++)
            Assert.Equal(elems[i], (string)((Atom)head[i]).Value);
    }
    #endregion

    #region Heap allocation
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    public void HeapAllocation(int termKind, int expectedCells)
    {
        var vm = SetupVM();
        var h0 = vm.H;
        Lang.Ast.Term term = termKind switch {
            0 => (__string)"hello",
            1 => new Variable("X"),
            2 => new Complex((__string)"f", (__string)"a", (__string)"b"),
            _ => throw new Exception()
        };
        vm.WriteHeapTerm(term);
        Assert.Equal(h0 + expectedCells, vm.H);
    }
    #endregion

    #region Hook
    [Theory]
    [InlineData("emitter_tests", "parent", "john", "mary", true)]
    [InlineData("emitter_tests", "parent", "mary", "susan", true)]
    [InlineData("emitter_tests", "parent", "john", "susan", false)]
    [InlineData("emitter_tests", "parent", "susan", "john", false)]
    public void Hook_Fire(string module, string functor, string a, string b, bool expected)
    {
        var kb = Consult(module);
        var vm = new ErgoVM();
        var hook = kb.Hook((__string)functor / 2).GetOrThrow();
        Assert.Equal(expected, hook.Fire(vm, (__string)a, (__string)b));
    }

    [Theory]
    [InlineData("emitter_tests", "parent", "john", "mary")]
    [InlineData("emitter_tests", "parent", "mary", "susan")]
    public void Hook_Call(string module, string functor, string arg, string expectedResult)
    {
        var kb = Consult(module);
        var vm = new ErgoVM();
        var hook = kb.Hook((__string)functor / 2).GetOrThrow();
        var result = hook.Call(vm, (__string)arg);
        Assert.NotNull(result);
        Assert.Equal(expectedResult, (string)((Atom)result).Value);
    }

    [Theory]
    [InlineData("emitter_tests", "parent", "nonexistent")]
    [InlineData("emitter_tests", "parent", "susan")]
    public void Hook_Call_Fails(string module, string functor, string arg)
    {
        var kb = Consult(module);
        var vm = new ErgoVM();
        var hook = kb.Hook((__string)functor / 2).GetOrThrow();
        var result = hook.Call(vm, (__string)arg);
        Assert.Null(result);
    }
    #endregion
}




