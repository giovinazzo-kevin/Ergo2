using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class DynamicTests : Tests
{
    private (KnowledgeBase kb, ErgoVM vm) Setup()
    {
        var kb = Consult("emitter_tests");
        var vm = new ErgoVM();
        var q = CompileQuery(kb, "fact");
        vm.open_query(q);
        vm.next_solution();
        vm.close_query();
        return (kb, vm);
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
        var q = CompileQuery(kb, $"assert({functor}({string.Join(", ", args)}))");
        vm.open_query(q);
        vm.next_solution();
        vm.close_query();
        AssertSolutions(vm.findall(CompileQuery(kb, query)), expected);
    }

    [Theory]
    [InlineData("color", new[] { "red", "green", "blue" })]
    [InlineData("num", new[] { "1", "2", "3", "4" })]
    public void Assert_MultipleFacts(string functor, string[] values)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, functor, 1);
        foreach (var v in values) {
            var q = CompileQuery(kb, $"assert({functor}({v}))");
            vm.open_query(q);
            vm.next_solution();
            vm.close_query();
        }
        var solutions = vm.findall(CompileQuery(kb, $"{functor}(X)")).ToList();
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
        foreach (var item in new[] { "a", "b", "c" }) {
            var q = CompileQuery(kb, $"assert(item({item}))");
            vm.open_query(q);
            vm.next_solution();
            vm.close_query();
        }
        var rq = CompileQuery(kb, $"retract(item({retract}))");
        vm.open_query(rq);
        vm.next_solution();
        vm.close_query();
        var solutions = vm.findall(CompileQuery(kb, "item(X)")).ToList();
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
        AssertSolutions(vm.findall(CompileQuery(kb, query)), expected);
    }

    [Theory]
    [InlineData("assert((grandparent(X, Z) :- parent(X, Y), parent(Y, Z)))", "grandparent(A, B)", new[] { "A/john, B/susan" })]
    public void Assert_Rule(string assertQuery, string query, string[] expected)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "grandparent", 2);
        var q = CompileQuery(kb, assertQuery);
        vm.open_query(q);
        vm.next_solution();
        vm.close_query();
        AssertSolutions(vm.findall(CompileQuery(kb, query)), expected);
    }

    [Theory]
    [InlineData("parent(john, X)", new[] { "X/mary" })]
    public void Static_NotShadowed(string query, string[] expected)
    {
        var (kb, vm) = Setup();
        AssertSolutions(vm.findall(CompileQuery(kb, query)), expected);
    }
}
