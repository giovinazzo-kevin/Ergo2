using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;
using System.Diagnostics;

namespace Ergo.UnitTests;

public class BuiltInTests : Tests
{
    [Fact]
    public void WriteBuiltIn_Prints()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var vm = new ErgoVM();
        var output = new List<string>();

        vm.RegisterBuiltIn(kb, "write", 1, static (vm) =>
        {
            var term = (Term)vm.A[0];
            var text = vm.Pretty(term);
            Trace.WriteLine($"WRITE: {text}");
            // stash in a static? no — use the event pattern
        });

        // For now just verify it doesn't crash
        var query = kb.Query("parent(X, mary), write(X)");
        vm.Run(query);
    }

    [Fact]
    public void WriteBuiltIn_OutputsCorrectValue()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var vm = new ErgoVM();
        var output = new List<string>();

        vm.RegisterBuiltIn(kb, "my_write", 1, (vm) =>
        {
            var raw = vm.A[0];
            var term = (Term)raw;
            var addr = term.Tag == Term.__TAG.REF ? vm.deref(term.Value) : -1;
            var resolved = addr >= 0 ? (Term)vm.Store[addr] : term;
            Trace.WriteLine($"DIAG: raw={raw} tag={term.Tag} val={term.Value} deref={addr} resolved_tag={resolved.Tag} resolved_val={resolved.Value}");
            output.Add(vm.Pretty(term));
        });

        var query = kb.Query("parent(X, mary), my_write(X)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(query);

        Assert.True(output.Count > 0, "Builtin never fired");
        Assert.Equal("john", output[0]);
    }

    [Fact]
    public void WriteBuiltIn_BacktracksCorrectly()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var vm = new ErgoVM();
        var output = new List<string>();

        vm.RegisterBuiltIn(kb, "my_write", 1, (vm) =>
        {
            var term = (Term)vm.A[0];
            output.Add(vm.Pretty(term));
        });

        // parent(X, Y) has 2 solutions — write should fire for each
        var query = kb.Query("parent(X, Y), my_write(X)");
        vm.Run(query);

        Assert.Equal(2, output.Count);
        Assert.Equal("john", output[0]);
        Assert.Equal("mary", output[1]);
    }

    [Fact]
    public void FailingBuiltIn_CausesBacktrack()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var vm = new ErgoVM();

        // A builtin that always fails
        vm.RegisterBuiltIn(kb, "always_fail", 0, static (vm) =>
        {
            vm.fail = true;
        });

        var query = kb.Query("fact, always_fail");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(query);

        Assert.Equal(0, solutions);
    }
}
