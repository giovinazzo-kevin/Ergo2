using Ergo.Lang.Ast;
using Ergo.Libs.Set.Ast;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class SetCollectionTests : CollectionTests<Set>
{
    protected override string Module => "list_tests";
    protected override string OpenDelim => "{";
    protected override string CloseDelim => "}";
    protected override Term EmptyElement => Ergo.Libs.Set.WellKnown.EmptySet;

    protected override Set MakeCollection(Term[] elements, Term tail) => new(elements, tail);
    protected override Set MakeEmpty() => new([], Ergo.Libs.Set.WellKnown.EmptySet);

    [Fact]
    public void Normalize_SortsElements()
    {
        var set = new Set([(__string)"c", (__string)"a", (__string)"b"]);
        var head = set.Head.ToArray();
        Assert.Equal("a", (string)((Atom)head[0]).Value);
        Assert.Equal("b", (string)((Atom)head[1]).Value);
        Assert.Equal("c", (string)((Atom)head[2]).Value);
    }

    [Fact]
    public void Normalize_Deduplicates()
    {
        var set = new Set([(__string)"a", (__string)"b", (__string)"a"]);
        var head = set.Head.ToArray();
        Assert.Equal(2, head.Length);
    }

    [Fact]
    public void Normalize_SortsThenDeduplicates()
    {
        var set = new Set([(__string)"c", (__string)"a", (__string)"b", (__string)"a"]);
        var head = set.Head.ToArray();
        Assert.Equal(3, head.Length);
        Assert.Equal("a", (string)((Atom)head[0]).Value);
        Assert.Equal("b", (string)((Atom)head[1]).Value);
        Assert.Equal("c", (string)((Atom)head[2]).Value);
    }

    [Fact]
    public void Unify_SameElements_DifferentOrder_Succeeds()
    {
        var kb = Consult(Module);
        var vm = new ErgoVM();
        var q = CompileQuery(kb, "{b, a} = {a, b}");
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += _ => solutions.Add(vm.MaterializeSolution());
        vm.Run(q);
        Assert.Single(solutions);
    }

    [Fact]
    public void Roundtrip_PreservesOrder()
    {
        var kb = Consult(Module);
        var vm = new ErgoVM();
        vm._QUERY = new Ergo.Compiler.Emission.Query(kb.Bytecode.AsQuery(), [], kb);
        var set = new Set([(__string)"c", (__string)"a", (__string)"b"]);
        var word = vm.WriteHeapTerm(set);
        var addr = vm.H;
        vm.Heap[vm.H++] = word;
        var read = vm.ReadHeapTerm(addr);
        Assert.IsType<Set>(read);
        var head = ((Set)read).Head.ToArray();
        // Should be sorted: a, b, c
        Assert.Equal("a", (string)((Atom)head[0]).Value);
        Assert.Equal("b", (string)((Atom)head[1]).Value);
        Assert.Equal("c", (string)((Atom)head[2]).Value);
    }
}
