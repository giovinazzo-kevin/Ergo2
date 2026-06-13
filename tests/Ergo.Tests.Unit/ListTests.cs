using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class ListTests : Tests
{
    private const string MODULE = "list_tests";

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

    [Theory]
    [InlineData("append([], [a, b], X)", new[] { "X/[a, b]" })]
    [InlineData("append([a, b], [], X)", new[] { "X/[a, b]" })]
    [InlineData("append([a], [b, c], X)", new[] { "X/[a, b, c]" })]
    [InlineData("append([], [], X)", new[] { "X/[]" })]
    [InlineData("member(X, [a, b, c])", new[] { "X/a", "X/b", "X/c" })]
    public void Query(string query, string[] expected)
    {
        AssertSolutions(Run(query), expected);
    }

    [Theory]
    [InlineData("member(b, [a, b, c])", 1)]
    [InlineData("member(d, [a, b, c])", 0)]
    public void Ground(string query, int expected)
    {
        Assert.Equal(expected, Run(query).Count);
    }
}

