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
        vm._QUERY = new Ergo.Compiler.Emission.Query(kb.Bytecode.AsQuery(), [], kb);
        return vm;
    }

    private Lang.Ast.Term Roundtrip(ErgoVM vm, Lang.Ast.Term term)
    {
        var word = vm.WriteHeapTerm(term);
        var addr = vm.H;
        vm.Heap[vm.H++] = word;
        return vm.ReadHeapTerm(addr);
    }

    private List<ErgoVM.Solution> Run(string query)
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var q = CompileQuery(kb, query);
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += _ => solutions.Add(vm.MaterializeSolution());
        vm.Run(q);
        return solutions;
    }

    #region AST
    [Fact]
    public void Ast_SortsByKey()
    {
        var dict = new Dict(
            (__string)"event",
            [
                new BinaryExpression(Operators.Module, (__string)"z", (__string)"1"),
                new BinaryExpression(Operators.Module, (__string)"a", (__string)"2")
            ]);
        Assert.Equal("a", (string)((Atom)dict.Pairs[0].Lhs).Value);
        Assert.Equal("z", (string)((Atom)dict.Pairs[1].Lhs).Value);
    }

    [Fact]
    public void Ast_Expl()
    {
        var dict = new Dict(
            (__string)"event",
            [new BinaryExpression(Operators.Module, (__string)"type", (__string)"click")]);
        Assert.Equal("event{type: click}", dict.Expl);
    }

    [Fact]
    public void Ast_Empty()
    {
        var dict = new Dict((__string)"empty", []);
        Assert.Equal("empty{}", dict.Expl);
    }
    #endregion

    #region Roundtrip
    [Fact]
    public void Roundtrip_SinglePair()
    {
        var vm = SetupVM();
        var dict = new Dict(
            (__string)"event",
            [new BinaryExpression(Operators.Module, (__string)"type", (__string)"click")]);
        var read = Roundtrip(vm, dict);
        Assert.IsType<Dict>(read);
        var rd = (Dict)read;
        Assert.Equal("event", (string)((Atom)rd.DictFunctor).Value);
        Assert.Single(rd.Pairs);
        Assert.Equal("type", (string)((Atom)rd.Pairs[0].Lhs).Value);
        Assert.Equal("click", (string)((Atom)rd.Pairs[0].Rhs).Value);
    }

    [Fact]
    public void Roundtrip_MultiplePairs()
    {
        var vm = SetupVM();
        var dict = new Dict(
            (__string)"point",
            [
                new BinaryExpression(Operators.Module, (__string)"x", (__int)10),
                new BinaryExpression(Operators.Module, (__string)"y", (__int)20)
            ]);
        var read = Roundtrip(vm, dict);
        Assert.IsType<Dict>(read);
        var rd = (Dict)read;
        Assert.Equal(2, rd.Pairs.Length);
    }

    [Fact]
    public void Roundtrip_Empty()
    {
        var vm = SetupVM();
        var dict = new Dict((__string)"empty", []);
        var read = Roundtrip(vm, dict);
        Assert.IsType<Dict>(read);
        var rd = (Dict)read;
        Assert.Empty(rd.Pairs);
    }
    #endregion

    #region Unification
    [Fact]
    public void Unify_SameKeys_Succeeds()
    {
        var vm = SetupVM();
        var d1 = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]);
        var d2 = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]);
        var w1 = vm.WriteHeapTerm(d1);
        var a1 = vm.H; vm.Heap[vm.H++] = w1;
        var w2 = vm.WriteHeapTerm(d2);
        var a2 = vm.H; vm.Heap[vm.H++] = w2;
        vm.unify(a1, a2);
        Assert.False(vm.fail);
    }

    [Fact]
    public void Unify_SubsetKeys_Succeeds()
    {
        var vm = SetupVM();
        var small = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]);
        var large = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"1"),
            new BinaryExpression(Operators.Module, (__string)"b", (__string)"2")]);
        var w1 = vm.WriteHeapTerm(small);
        var a1 = vm.H; vm.Heap[vm.H++] = w1;
        var w2 = vm.WriteHeapTerm(large);
        var a2 = vm.H; vm.Heap[vm.H++] = w2;
        vm.unify(a1, a2);
        Assert.False(vm.fail);
    }

    [Fact]
    public void Unify_MissingKey_Fails()
    {
        var vm = SetupVM();
        // e{x: 1} vs e{y: 1} — key x not in second dict
        var d1 = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"x", (__string)"1")]);
        var d2 = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"y", (__string)"1")]);
        var w1 = vm.WriteHeapTerm(d1);
        var a1 = vm.H; vm.Heap[vm.H++] = w1;
        var w2 = vm.WriteHeapTerm(d2);
        var a2 = vm.H; vm.Heap[vm.H++] = w2;
        vm.unify(a1, a2);
        Assert.True(vm.fail);
    }

    [Fact]
    public void Unify_DifferentFunctor_Fails()
    {
        var vm = SetupVM();
        var d1 = new Dict((__string)"click", [
            new BinaryExpression(Operators.Module, (__string)"x", (__int)10)]);
        var d2 = new Dict((__string)"hover", [
            new BinaryExpression(Operators.Module, (__string)"x", (__int)10)]);
        var w1 = vm.WriteHeapTerm(d1);
        var a1 = vm.H; vm.Heap[vm.H++] = w1;
        var w2 = vm.WriteHeapTerm(d2);
        var a2 = vm.H; vm.Heap[vm.H++] = w2;
        vm.unify(a1, a2);
        Assert.True(vm.fail);
    }

    [Fact]
    public void Unify_ValueMismatch_Fails()
    {
        var vm = SetupVM();
        var d1 = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"1")]);
        var d2 = new Dict((__string)"e", [
            new BinaryExpression(Operators.Module, (__string)"a", (__string)"2")]);
        var w1 = vm.WriteHeapTerm(d1);
        var a1 = vm.H; vm.Heap[vm.H++] = w1;
        var w2 = vm.WriteHeapTerm(d2);
        var a2 = vm.H; vm.Heap[vm.H++] = w2;
        vm.unify(a1, a2);
        Assert.True(vm.fail);
    }
    #endregion
}
