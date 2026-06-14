using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class DictParseTests : Tests
{
    [Fact]
    public void Parse_Dict_In_Query()
    {
        var kb = Consult("list_tests");
        var vm = new ErgoVM();
        var q = CompileQuery(kb, "X = event{type: click}");
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += _ => solutions.Add(vm.MaterializeSolution());
        vm.Run(q);
        Assert.Single(solutions);
    }

    [Fact]
    public void Parse_Dict_Fact_And_Query()
    {
        var kb = Consult("dict_tests");
        var vm = new ErgoVM();
        var q = CompileQuery(kb, "contento(X, H)");
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += _ => solutions.Add(vm.MaterializeSolution());
        vm.Run(q);
        Assert.NotEmpty(solutions);
        Assert.Equal("fox", solutions[0].Bindings[0].Value.Expl);
        Assert.Equal("postgres", solutions[0].Bindings[1].Value.Expl);
    }
}
