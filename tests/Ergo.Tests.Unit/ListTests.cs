using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;
using System.Diagnostics;

namespace Ergo.UnitTests;

public class ListTests : Tests
{
    private KnowledgeBase KB() => Consult("list_tests");

    private List<ErgoVM.Solution> Run(string query)
    {
        var kb = KB();
        var vm = new ErgoVM();
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += _ => solutions.Add(vm.MaterializeSolution());
        vm.Run(kb.Query(query));
        return solutions;
    }

    #region append/3
    [Fact]
    public void Append_EmptyToList()
    {
        var solutions = Run("append([], [a, b], X)");
        Assert.Single(solutions);
        Trace.WriteLine($"X = {solutions[0].Bindings[0].Value.Expl}");
        Assert.Equal("[a, b]", solutions[0].Bindings[0].Value.Expl);
    }

    [Fact]
    public void Append_ListToEmpty()
    {
        var solutions = Run("append([a, b], [], X)");
        Assert.Single(solutions);
        Trace.WriteLine($"X = {solutions[0].Bindings[0].Value.Expl}");
        Assert.Equal("[a, b]", solutions[0].Bindings[0].Value.Expl);
    }

    [Fact]
    public void Append_TwoLists()
    {
        var solutions = Run("append([a], [b, c], X)");
        Assert.Single(solutions);
        Trace.WriteLine($"X = {solutions[0].Bindings[0].Value.Expl}");
        Assert.Equal("[a, b, c]", solutions[0].Bindings[0].Value.Expl);
    }

    [Fact]
    public void Append_BothEmpty()
    {
        var solutions = Run("append([], [], X)");
        Assert.Single(solutions);
        Trace.WriteLine($"X = {solutions[0].Bindings[0].Value.Expl}");
        Assert.Equal("[]", solutions[0].Bindings[0].Value.Expl);
    }
    #endregion

    #region member/2
    [Fact]
    public void Member_Found()
    {
        var solutions = Run("member(b, [a, b, c])");
        Assert.True(solutions.Count >= 1);
    }

    [Fact]
    public void Member_NotFound()
    {
        var solutions = Run("member(d, [a, b, c])");
        Assert.Empty(solutions);
    }

    [Fact]
    public void Member_EnumeratesAll()
    {
        var solutions = Run("member(X, [a, b, c])");
        Assert.Equal(3, solutions.Count);
        Trace.WriteLine($"X0 = {solutions[0].Bindings[0].Value.Expl}");
        Trace.WriteLine($"X1 = {solutions[1].Bindings[0].Value.Expl}");
        Trace.WriteLine($"X2 = {solutions[2].Bindings[0].Value.Expl}");
        Assert.Equal("a", solutions[0].Bindings[0].Value.Expl);
        Assert.Equal("b", solutions[1].Bindings[0].Value.Expl);
        Assert.Equal("c", solutions[2].Bindings[0].Value.Expl);
    }
    #endregion
}
