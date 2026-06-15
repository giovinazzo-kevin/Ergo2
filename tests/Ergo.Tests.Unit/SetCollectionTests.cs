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

    [Fact] public void Normalize_SortsElements()
    {
        var h = new Set([(__string)"c", (__string)"a", (__string)"b"]).Head.ToArray();
        Assert.Equal("a", (string)((Atom)h[0]).Value);
        Assert.Equal("b", (string)((Atom)h[1]).Value);
        Assert.Equal("c", (string)((Atom)h[2]).Value);
    }
    [Fact] public void Normalize_Deduplicates()
        => Assert.Equal(2, new Set([(__string)"a", (__string)"b", (__string)"a"]).Head.Count());
    [Fact] public void Normalize_SortsThenDeduplicates()
    {
        var h = new Set([(__string)"c", (__string)"a", (__string)"b", (__string)"a"]).Head.ToArray();
        Assert.Equal(3, h.Length);
        Assert.Equal("a", (string)((Atom)h[0]).Value);
    }
    [Fact] public void Unify_SameElements_DifferentOrder_Succeeds()
        => Assert.Single(new ErgoVM().findall(CompileQuery(Consult(Module), "{b, a} = {a, b}")));
    [Fact] public void Roundtrip_PreservesOrder()
    {
        var kb = Consult(Module);
        var vm = new ErgoVM();
        vm._QUERY = new Ergo.Compiler.Emission.Query(kb.Bytecode.AsQuery(), [], kb);
        var w = vm.WriteHeapTerm(new Set([(__string)"c", (__string)"a", (__string)"b"]));
        var a = vm.H; vm.Heap[vm.H++] = w;
        var h = ((Set)vm.ReadHeapTerm(a)).Head.ToArray();
        Assert.Equal("a", (string)((Atom)h[0]).Value);
        Assert.Equal("b", (string)((Atom)h[1]).Value);
        Assert.Equal("c", (string)((Atom)h[2]).Value);
    }
}
