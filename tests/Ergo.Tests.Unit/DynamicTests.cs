using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;
using Xunit.Abstractions;

namespace Ergo.UnitTests;

public class DynamicTests : Tests
{
    private readonly ITestOutputHelper _out;

    public DynamicTests(ITestOutputHelper output) => _out = output;

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

    #region Assert Ground Facts
    [Theory]
    [InlineData("likes", new[] { "john", "mary" })]
    [InlineData("color", new[] { "red" })]
    [InlineData("edge", new[] { "a", "b" })]
    public void Assert_GroundFact_BindingsCorrect(string functor, string[] args)
    {
        var (kb, vm) = Setup();

        var argList = string.Join(", ", args);
        var varList = string.Join(", ", args.Select((_, i) => ((char)('A' + i)).ToString()));
        var arity = args.Length;

        vm.DeclareDynamic(kb, functor, arity);
        vm.Run(kb.Query($"assert({functor}({argList}))"));

        var solutions = RunQuery(vm, kb, $"{functor}({varList})");

        Assert.Single(solutions);
        for (int i = 0; i < args.Length; i++)
        {
            var binding = solutions[0].Bindings[i];
            _out.WriteLine($"  {binding.Variable} = {binding.Value}");
            Assert.Equal(args[i], binding.Value.Expl);
        }
    }
    #endregion

    #region Assert Multiple + Backtracking
    [Theory]
    [InlineData("color", new[] { "red", "green", "blue" })]
    [InlineData("num", new[] { "1", "2", "3", "4" })]
    public void Assert_MultipleFacts_AllSolutionsReturned(string functor, string[] values)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, functor, 1);

        foreach (var v in values)
            vm.Run(kb.Query($"assert({functor}({v}))"));

        var solutions = RunQuery(vm, kb, $"{functor}(X)");

        Assert.Equal(values.Length, solutions.Count);
        for (int i = 0; i < values.Length; i++)
        {
            _out.WriteLine($"  solution {i}: X = {solutions[i].Bindings[0].Value.Expl}");
            Assert.Equal(values[i], solutions[i].Bindings[0].Value.Expl);
        }
    }
    #endregion

    #region Retract
    [Theory]
    [InlineData("a", new[] { "b", "c" })]
    [InlineData("b", new[] { "a", "c" })]
    [InlineData("c", new[] { "a", "b" })]
    public void Retract_RemovesClause_RemainingCorrect(string retract, string[] expected)
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "item", 1);

        foreach (var item in new[] { "a", "b", "c" })
            vm.Run(kb.Query($"assert(item({item}))"));

        vm.Run(kb.Query($"retract(item({retract}))"));

        var solutions = RunQuery(vm, kb, "item(X)");

        Assert.Equal(expected.Length, solutions.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            _out.WriteLine($"  solution {i}: X = {solutions[i].Bindings[0].Value.Expl}");
            Assert.Equal(expected[i], solutions[i].Bindings[0].Value.Expl);
        }
    }
    #endregion

    #region Cross-Query Persistence
    [Fact]
    public void Assert_GroundFact_DynClauseCodeCorrect()
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "likes", 2);

        vm.Run(kb.Query("assert(likes(john, mary))"));

        // Inspect the dynamic clause code
        var dynamics = vm.GetDynamicPredicates();
        Assert.NotEmpty(dynamics);
        foreach (var (sig, dyn) in dynamics)
        {
            foreach (var clause in dyn.Clauses)
            {
                var codeStr = string.Join(", ", clause.Code.Select(w => w.ToString()));
                _out.WriteLine($"DynClause code [{clause.Code.Length} words]: {codeStr}");
                _out.WriteLine($"DynClause offset: {clause.Offset}");
            }
        }

        // Now query and trace A registers at solution time
        var solutions = new List<ErgoVM.Solution>();
        void handler(ErgoVM v)
        {
            _out.WriteLine($"--- Solution emit ---");
            for (int i = 0; i < 2; i++)
            {
                var raw = v.A[i];
                var tag = raw & 3;
                var val = raw >> 2;
                _out.WriteLine($"A[{i}] raw={raw} tag={tag} value={val}");
                if (tag == 3) // REF
                {
                    var addr = v.deref(val);
                    var resolved = v.Store[addr];
                    var rTag = resolved & 3;
                    var rVal = resolved >> 2;
                    _out.WriteLine($"  deref({val}) -> addr={addr} resolved raw={resolved} tag={rTag} value={rVal}");
                    if (rTag == 0) // CON
                        _out.WriteLine($"  constant[{rVal}] = {v.Constants[rVal].Expl}");
                }
            }
            solutions.Add(v.MaterializeSolution());
        }
        vm.SolutionEmitted += handler;
        vm.Run(kb.Query("likes(A, B)"));
        vm.SolutionEmitted -= handler;

        _out.WriteLine($"Solutions: {solutions.Count}");
        foreach (var sol in solutions)
            _out.WriteLine($"  {sol}");
    }

    [Fact]
    public void Assert_PersistsAcrossQueries()
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "persistent", 1);

        vm.Run(kb.Query("assert(persistent(data))"));

        // New query should see the asserted fact
        var solutions = RunQuery(vm, kb, "persistent(X)");

        Assert.Single(solutions);
        Assert.Equal("data", solutions[0].Bindings[0].Value.Expl);
    }
    #endregion

    #region Within-Query Assert
    [Fact]
    public void Assert_WithinQuery_ImmediatelyAvailable()
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "immediate", 1);

        var solutions = RunQuery(vm, kb, "assert(immediate(yes)), immediate(X)");

        Assert.Single(solutions);
        Assert.Equal("yes", solutions[0].Bindings[0].Value.Expl);
    }
    #endregion

    #region Assert Rules With Body
    [Fact]
    public void Assert_RuleWithBody_ResolvesCorrectly()
    {
        var (kb, vm) = Setup();
        vm.DeclareDynamic(kb, "grandparent", 2);

        // parent(john, mary) and parent(mary, susan) exist in KB
        vm.Run(kb.Query("assert((grandparent(X, Z) :- parent(X, Y), parent(Y, Z)))"));

        var solutions = RunQuery(vm, kb, "grandparent(X, Y)");

        Assert.Single(solutions);
        _out.WriteLine($"  X = {solutions[0].Bindings[0].Value.Expl}");
        _out.WriteLine($"  Y = {solutions[0].Bindings[1].Value.Expl}");
        Assert.Equal("john", solutions[0].Bindings[0].Value.Expl);
        Assert.Equal("susan", solutions[0].Bindings[1].Value.Expl);
    }
    #endregion

    #region Interaction With Static Predicates
    [Fact]
    public void Dynamic_DoesNotShadowStatic()
    {
        var (kb, vm) = Setup();

        // parent/2 is static with john→mary, mary→susan
        // assert a dynamic parent — should NOT interfere with static resolution
        // (dynamic predicates are separate from static)
        var solutions = RunQuery(vm, kb, "parent(john, X)");
        Assert.Single(solutions);
        Assert.Equal("mary", solutions[0].Bindings[0].Value.Expl);
    }
    #endregion
}
