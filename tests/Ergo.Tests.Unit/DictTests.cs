using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Libs.Dict.Ast;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class DictTests : Tests
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
        var w = vm.write_heap_term(term); var a = vm.H; vm.Heap[vm.H++] = w;
        return vm.read_heap_term(a);
    }

    private void UnifyDicts(ErgoVM vm, Lang.Ast.Term d1, Lang.Ast.Term d2)
    {
        var w1 = vm.write_heap_term(d1); var a1 = vm.H; vm.Heap[vm.H++] = w1;
        var w2 = vm.write_heap_term(d2); var a2 = vm.H; vm.Heap[vm.H++] = w2;
        vm.unify(a1, a2);
    }

    [Fact] public void Ast_SortsByKey()
    {
        var d = new Dict((__string)"event", [
            new BinaryExpression(Operators.Module, (__string)"z", (__string)"1"),
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"2")]);
        Assert.Equal("a", (string)((Atom)d.Pairs[0].Lhs).Value);
    }
    [Fact] public void Ast_Expl() => Assert.Equal("event{type: click}",
        new Dict((__string)"event", [new BinaryExpression(Operators.Module, (__string)"type", (__string)"click")]).Expl);
    [Fact] public void Ast_Empty() => Assert.Equal("empty{}", new Dict((__string)"empty", []).Expl);

    [Fact] public void Roundtrip_SinglePair()
    {
        var vm = SetupVM();
        var rd = (Dict)Roundtrip(vm, new Dict((__string)"event",
            [new BinaryExpression(Operators.Module, (__string)"type", (__string)"click")]));
        Assert.Equal("event", (string)((Atom)rd.DictFunctor).Value);
        Assert.Single(rd.Pairs);
    }
    [Fact] public void Roundtrip_MultiplePairs()
        => Assert.Equal(2, ((Dict)Roundtrip(SetupVM(), new Dict((__string)"point", [
            new BinaryExpression(Operators.Module, (__string)"x", (__int)10),
            new BinaryExpression(Operators.Module, (__string)"y", (__int)20)]))).Pairs.Length);
    [Fact] public void Roundtrip_Empty()
        => Assert.Empty(((Dict)Roundtrip(SetupVM(), new Dict((__string)"empty", []))).Pairs);

    [Fact] public void Unify_SameKeys_Succeeds() { var vm = SetupVM(); UnifyDicts(vm,
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]),
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")])); Assert.False(vm.fail); }
    [Fact] public void Unify_SubsetKeys_Succeeds() { var vm = SetupVM(); UnifyDicts(vm,
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]),
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"a", (__string)"1"),
            new BinaryExpression(Operators.Module, (__string)"b", (__string)"2")])); Assert.False(vm.fail); }
    [Fact] public void Unify_MissingKey_Fails() { var vm = SetupVM(); UnifyDicts(vm,
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"x", (__string)"1")]),
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"y", (__string)"1")])); Assert.True(vm.fail); }
    [Fact] public void Unify_DifferentFunctor_Fails() { var vm = SetupVM(); UnifyDicts(vm,
        new Dict((__string)"click", [new BinaryExpression(Operators.Module, (__string)"x", (__int)10)]),
        new Dict((__string)"hover", [new BinaryExpression(Operators.Module, (__string)"x", (__int)10)])); Assert.True(vm.fail); }
    [Fact] public void Unify_ValueMismatch_Fails() { var vm = SetupVM(); UnifyDicts(vm,
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]),
        new Dict((__string)"e", [new BinaryExpression(Operators.Module, (__string)"a", (__string)"2")])); Assert.True(vm.fail); }
}
