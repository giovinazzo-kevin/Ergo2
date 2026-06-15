using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Runtime.WAM;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;
using Signature = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.UnitTests;

public class BuiltInTests : Tests
{
    private const string MODULE = "emitter_tests";

    #region read_heap_term
    [Theory]
    [InlineData("parent(john, mary)")]
    [InlineData("parent(mary, susan)")]
    [InlineData("complex_fact(0, 1, 2)")]
    public void read_heap_term_ReconstructsGroundFact(string query)
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var solutions = vm.findall(CompileQuery(kb, query));
        Assert.Single(solutions);
    }

    [Theory]
    [InlineData("assert((grandparent(X, Z) :- parent(X, Y), parent(Y, Z)))", "grandparent(A, B)", new[] { "A/john, B/susan" })]
    public void read_heap_term_ReconstructsClause(string assertQuery, string query, string[] expected)
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        vm.DeclareDynamic(kb, "grandparent", 2);
        var q = CompileQuery(kb, assertQuery);
        vm.open_query(q);
        vm.next_solution();
        vm.close_query();
        AssertSolutions(vm.findall(CompileQuery(kb, query)), expected);
    }
    #endregion

    [Theory]
    [InlineData("parent(X, mary), my_write(X)", new[] { "john" })]
    [InlineData("parent(X, Y), my_write(X)", new[] { "john", "mary" })]
    public void WriteBuiltIn_OutputsCorrectValues(string query, string[] expectedOutput)
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var output = new List<string>();
        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)(vm => output.Add(vm.pretty(vm.A[0]))));
        foreach (var _ in vm.findall(CompileQuery(kb, query))) { }
        Assert.Equal(expectedOutput, output.ToArray());
    }

    #region call/N
    [Theory]
    [InlineData("call(parent(john, X))", new[] { "X/mary" })]
    [InlineData("call(parent(john), X)", new[] { "X/mary" })]
    [InlineData("call(fact)", new[] { "" })]
    [InlineData("call(parent(X, Y))", new[] { "X/john, Y/mary", "X/mary, Y/susan" })]
    public void Call_DispatchesCorrectly(string query, string[] expected)
        => AssertSolutions(new ErgoVM().findall(CompileQuery(Consult(MODULE), query)), expected);
    #endregion

    [Fact]
    public void FailingBuiltIn_CausesBacktrack()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        kb.RegisterBuiltInLabel("always_fail", 0, (ErgoVM.__op)(vm => vm.fail = true));
        Assert.Empty(vm.findall(CompileQuery(kb, "fact, always_fail")));
    }
}
