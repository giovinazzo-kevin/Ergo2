using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class DynamicTests : Tests
{
    private (KnowledgeBase kb, ErgoVM vm) Setup()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var vm = new ErgoVM();
        vm.RegisterWellKnownOperators();
        return (kb, vm);
    }

    private List<ErgoVM.Solution> RunQuery(ErgoVM vm, KnowledgeBase kb, string query)
    {
        var solutions = new List<ErgoVM.Solution>();
        void handler(ErgoVM v) => solutions.Add(v.MaterializeSolution());
        vm.SolutionEmitted += handler;
        vm.Run(kb.Query(query));
        vm.SolutionEmitted -= handler;
        return solutions;
    }

    [Theory]
    [InlineData("likes", new[] { "john", "mary" }, "likes(A, B)", new[] { "A/john, B/mary" })]
    [InlineData("color", new[] { "red" }, "color(X)", new[] { "X/red" })]
    [InlineData("edge", new[] { "a", "b" }, "edge(A, B)", new[] { "A/a, B/b" })]
    [InlineData("persistent", new[] { "data" }, "persistent(X)", new[] { "X/data" })]
    public void Assert_Query(string functor, string[] args, string query, string[] expected)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, functor, args.Length);
        vm.Run(kb.Query($"assert({functor}({string.Join(", ", args)}))"));
        AssertSolutions(RunQuery(vm, kb, query), expected);
    }

    [Theory]
    [InlineData("color", new[] { "red", "green", "blue" })]
    [InlineData("num", new[] { "1", "2", "3", "4" })]
    public void Assert_MultipleFacts(string functor, string[] values)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, functor, 1);
        foreach (var v in values)
            vm.Run(kb.Query($"assert({functor}({v}))"));
        var solutions = RunQuery(vm, kb, $"{functor}(X)");
        Assert.Equal(values.Length, solutions.Count);
        for (int i = 0; i < values.Length; i++)
            Assert.Equal(values[i], solutions[i].Bindings[0].Value.Expl);
    }

    [Theory]
    [InlineData("a", new[] { "b", "c" })]
    [InlineData("b", new[] { "a", "c" })]
    [InlineData("c", new[] { "a", "b" })]
    public void Retract(string retract, string[] expected)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "item", 1);
        foreach (var item in new[] { "a", "b", "c" })
            vm.Run(kb.Query($"assert(item({item}))"));
        vm.Run(kb.Query($"retract(item({retract}))"));
        var solutions = RunQuery(vm, kb, "item(X)");
        Assert.Equal(expected.Length, solutions.Count);
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], solutions[i].Bindings[0].Value.Expl);
    }

    [Theory]
    [InlineData("assert(immediate(yes)), immediate(X)", new[] { "X/yes" })]
    public void Assert_WithinQuery(string query, string[] expected)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "immediate", 1);
        AssertSolutions(RunQuery(vm, kb, query), expected);
    }

    [Theory]
    [InlineData("assert((grandparent(X, Z) :- parent(X, Y), parent(Y, Z)))", "grandparent(A, B)", new[] { "A/john, B/susan" })]
    public void Assert_Rule(string assertQuery, string query, string[] expected)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "grandparent", 2);
        vm.Run(kb.Query(assertQuery));
        AssertSolutions(RunQuery(vm, kb, query), expected);
    }

    [Theory]
    [InlineData("parent(john, X)", new[] { "X/mary" })]
    public void Static_NotShadowed(string query, string[] expected)
    {
        var (kb, vm) = Setup();
        AssertSolutions(RunQuery(vm, kb, query), expected);
    }
}
