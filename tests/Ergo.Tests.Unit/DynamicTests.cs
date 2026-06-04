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
        vm.RegisterDynamicBuiltIns(kb);
        return (kb, vm);
    }

    [Fact]
    public void Assert_AddsConstantToKB()
    {
        var (kb, vm) = Setup();

        // Verify assert builtin actually fires
        bool builtinFired = false;
        string? traceInfo = null;
        vm.RegisterBuiltIn(kb, "test_assert", 1, v => {
            builtinFired = true;
            try
            {
                var addr = v.deref(ErgoVM.HEAP_SIZE + ErgoVM.STACK_SIZE);
                var term = v.ReadHeapTerm(addr);
                traceInfo = $"addr={addr}, term={term?.GetType().Name}: {term?.Expl ?? "null"}";
                v.AssertClause(0, atEnd: true);
                traceInfo += $" | after assert, KB has 'likes': {kb.Bytecode.ConstantsLookup.ContainsKey("likes")}";
            }
            catch (Exception ex)
            {
                traceInfo = $"EXCEPTION: {ex.GetType().Name}: {ex.Message}";
            }
        });

        var q1 = kb.Query("test_assert(likes(john, mary))");
        vm.Run(q1);

        Assert.True(builtinFired, "Builtin never fired");
        Assert.True(kb.Bytecode.ConstantsLookup.ContainsKey("likes"),
            $"trace: {traceInfo}\nKB constants: [{string.Join(", ", kb.Bytecode.ConstantsLookup.Keys)}]");
    }

    [Fact]
    public void Assert_GroundFact_ThenQuery()
    {
        var (kb, vm) = Setup();

        // Assert a new ground fact
        var q1 = kb.Query("assert(likes(john, mary))");
        vm.Run(q1);

        // Query it back
        var q2 = kb.Query("likes(john, mary)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(q2);

        Assert.Equal(1, solutions);
    }

    [Fact]
    public void Assert_MultipleFacts_BacktracksCorrectly()
    {
        var (kb, vm) = Setup();

        vm.Run(kb.Query("assert(color(red))"));
        vm.Run(kb.Query("assert(color(green))"));
        vm.Run(kb.Query("assert(color(blue))"));

        var q = kb.Query("color(X)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(q);

        Assert.Equal(3, solutions);
    }

    [Fact]
    public void Retract_RemovesFact()
    {
        var (kb, vm) = Setup();

        vm.Run(kb.Query("assert(temp(1))"));
        vm.Run(kb.Query("assert(temp(2))"));

        // Retract first clause
        vm.Run(kb.Query("retract(temp(_))"));

        var q = kb.Query("temp(X)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(q);

        // Only one should remain
        Assert.Equal(1, solutions);
    }

    [Fact]
    public void Assert_RuleWithBody_ThenQuery()
    {
        var (kb, vm) = Setup();

        // Assert: grandparent(X, Z) :- parent(X, Y), parent(Y, Z).
        // parent(john, mary) and parent(mary, susan) already exist in the KB
        vm.Run(kb.Query("assert((grandparent(X, Z) :- parent(X, Y), parent(Y, Z)))"));

        var q = kb.Query("grandparent(john, susan)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(q);

        Assert.Equal(1, solutions);
    }

    [Fact]
    public void Assert_WithinQuery_ImmediatelyAvailable()
    {
        var (kb, vm) = Setup();

        // Must declare dynamic before query compilation can resolve it
        vm.DeclareDynamic(kb, "immediate", 1);

        // Assert and query in the same execution
        var q = kb.Query("assert(immediate(yes)), immediate(X)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(q);

        Assert.Equal(1, solutions);
    }

    [Fact]
    public void Assert_PersistsAcrossQueries()
    {
        var (kb, vm) = Setup();

        vm.Run(kb.Query("assert(persistent(data))"));

        // New query should see the asserted fact
        var q = kb.Query("persistent(X)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(q);

        Assert.Equal(1, solutions);
    }
}
