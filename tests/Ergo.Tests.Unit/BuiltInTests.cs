using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Runtime.WAM;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;
using Term = Ergo.Compiler.Emission.Term;
using Signature = Ergo.Compiler.Emission.Signature;

namespace Ergo.UnitTests;

public class BuiltInTests : Tests
{
    #region Abstract Term Reconstruction
    [Fact]
    public void ReadHeapTerm_ReconstructsClauseWithConjunctionBody()
    {
        // Constants: 0="foo", 1="bar", 2="baz", 3=",", 4=":-"
        var constants = new Atom[]
        {
            (__string)"foo", (__string)"bar", (__string)"baz",
            (__string)",", (__string)":-"
        };

        var vm = new ErgoVM();
        vm.RegisterWellKnownOperators();
        vm._QUERY = QueryBytecode.Preloaded([], constants);

        // Heap layout for: foo(X) :- bar(X), baz(X)
        //   H[0]: foo/1      H[1]: REF(1) = X
        //   H[2]: bar/1      H[3]: REF(1) = X
        //   H[4]: baz/1      H[5]: REF(1) = X
        //   H[6]: ,/2        H[7]: STR(2)  H[8]: STR(4)
        //   H[9]: :-/2       H[10]: STR(0) H[11]: STR(6)
        //   H[12]: STR(9) = entry point
        var h = vm.Heap;
        h[0] = (Signature)(0, 1);    // foo/1
        h[1] = (Term)(REF, 1);       // X unbound
        h[2] = (Signature)(1, 1);    // bar/1
        h[3] = (Term)(REF, 1);       // same X
        h[4] = (Signature)(2, 1);    // baz/1
        h[5] = (Term)(REF, 1);       // same X
        h[6] = (Signature)(3, 2);    // ,/2
        h[7] = (Term)(STR, 2);       // -> bar(X)
        h[8] = (Term)(STR, 4);       // -> baz(X)
        h[9] = (Signature)(4, 2);    // :-/2
        h[10] = (Term)(STR, 0);      // -> foo(X)
        h[11] = (Term)(STR, 6);      // -> ,(bar(X), baz(X))
        h[12] = (Term)(STR, 9);      // entry point

        var result = vm.ReadHeapTerm(12);

        // Should be Ast.Clause
        Assert.IsType<Lang.Ast.Clause>(result);
        var clause = (Lang.Ast.Clause)result;

        // Head = foo(X)
        var head = clause.Functor;
        Assert.IsType<Complex>(head);
        Assert.Equal("foo", (string)((Complex)head).Functor.Value);

        // Goals = [bar(X), baz(X)]
        var goals = clause.Goals.ToArray();
        Assert.Equal(2, goals.Length);
        Assert.Equal("bar", (string)((Complex)goals[0]).Functor.Value);
        Assert.Equal("baz", (string)((Complex)goals[1]).Functor.Value);

        // All X vars share the same name (same heap address)
        var headX = (Variable)((Complex)head).Args[0];
        var barX = (Variable)((Complex)goals[0]).Args[0];
        var bazX = (Variable)((Complex)goals[1]).Args[0];
        Assert.Equal(headX.Name, barX.Name);
        Assert.Equal(headX.Name, bazX.Name);
    }

    [Fact]
    public void ReadHeapTerm_ReconstructsGroundFact()
    {
        // Constants: 0="parent", 1="john", 2="mary"
        var constants = new Atom[]
        {
            (__string)"parent", (__string)"john", (__string)"mary"
        };

        var vm = new ErgoVM();
        vm.RegisterWellKnownOperators();
        vm._QUERY = QueryBytecode.Preloaded([], constants);

        // Heap: parent(john, mary)
        //   H[0]: parent/2   H[1]: CON(1)=john  H[2]: CON(2)=mary
        //   H[3]: STR(0) = entry point
        var h = vm.Heap;
        h[0] = (Signature)(0, 2);    // parent/2
        h[1] = (Term)(CON, 1);       // john
        h[2] = (Term)(CON, 2);       // mary
        h[3] = (Term)(STR, 0);       // entry point

        var result = vm.ReadHeapTerm(3);

        // Should be a plain Complex (no operator reconstruction)
        Assert.IsType<Complex>(result);
        var complex = (Complex)result;
        Assert.Equal("parent", (string)complex.Functor.Value);
        Assert.Equal(2, complex.Args.Length);
        Assert.Equal("john", (string)((Atom)complex.Args[0]).Value);
        Assert.Equal("mary", (string)((Atom)complex.Args[1]).Value);
    }
    #endregion

    [Fact]
    public void WriteBuiltIn_Prints()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var vm = new ErgoVM();
        var output = new List<string>();

        kb.RegisterBuiltInLabel("write", 1, (ErgoVM.__op)((vm) =>
        {
            var term = (Term)vm.A[0];
            var text = vm.Pretty(term);
            Trace.WriteLine($"WRITE: {text}");
            // stash in a static? no — use the event pattern
        }));

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

        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)((vm) =>
        {
            var raw = vm.A[0];
            var term = (Term)raw;
            var addr = term.Tag == Term.__TAG.REF ? vm.deref(term.Value) : -1;
            var resolved = addr >= 0 ? (Term)vm.Store[addr] : term;
            Trace.WriteLine($"DIAG: raw={raw} tag={term.Tag} val={term.Value} deref={addr} resolved_tag={resolved.Tag} resolved_val={resolved.Value}");
            output.Add(vm.Pretty(term));
        }));

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

        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)((vm) =>
        {
            var term = (Term)vm.A[0];
            output.Add(vm.Pretty(term));
        }));

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
        kb.RegisterBuiltInLabel("always_fail", 0, (ErgoVM.__op)((vm) =>
        {
            vm.fail = true;
        }));

        var query = kb.Query("fact, always_fail");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(query);

        Assert.Equal(0, solutions);
    }
}
