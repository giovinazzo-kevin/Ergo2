using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Runtime.WAM;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;
using Signature = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;

namespace Ergo.UnitTests;

public class BuiltInTests : Tests
{
    private const string MODULE = "emitter_tests";

    #region Abstract Term Reconstruction
    [Fact]
    public void ReadHeapTerm_ReconstructsClauseWithConjunctionBody()
    {
        var constants = new Atom[]
        {
            (__string)"foo", (__string)"bar", (__string)"baz",
            (__string)",", (__string)":-"
        };

        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        vm.KB = kb;
        vm._QUERY = QueryBytecode.Preloaded([], constants);

        var h = vm.Heap;
        h[0] = (Signature)(0, 1);
        h[1] = (Term)(REF, 1);
        h[2] = (Signature)(1, 1);
        h[3] = (Term)(REF, 1);
        h[4] = (Signature)(2, 1);
        h[5] = (Term)(REF, 1);
        h[6] = (Signature)(3, 2);
        h[7] = (Term)(STR, 2);
        h[8] = (Term)(STR, 4);
        h[9] = (Signature)(4, 2);
        h[10] = (Term)(STR, 0);
        h[11] = (Term)(STR, 6);
        h[12] = (Term)(STR, 9);

        var result = vm.ReadHeapTerm(12);

        Assert.IsType<Clause>(result);
        var clause = (Clause)result;

        var head = clause.Functor;
        Assert.IsType<Complex>(head);
        Assert.Equal("foo", (string)((Complex)head).Functor.Value);

        var goals = clause.Goals.ToArray();
        Assert.Equal(2, goals.Length);
        Assert.Equal("bar", (string)((Complex)goals[0]).Functor.Value);
        Assert.Equal("baz", (string)((Complex)goals[1]).Functor.Value);

        var headX = (Variable)((Complex)head).Args[0];
        var barX = (Variable)((Complex)goals[0]).Args[0];
        var bazX = (Variable)((Complex)goals[1]).Args[0];
        Assert.Equal(headX.Name, barX.Name);
        Assert.Equal(headX.Name, bazX.Name);
    }

    [Fact]
    public void ReadHeapTerm_ReconstructsGroundFact()
    {
        var constants = new Atom[]
        {
            (__string)"parent", (__string)"john", (__string)"mary"
        };

        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        vm.KB = kb;
        vm._QUERY = QueryBytecode.Preloaded([], constants);

        var h = vm.Heap;
        h[0] = (Signature)(0, 2);
        h[1] = (Term)(CON, 1);
        h[2] = (Term)(CON, 2);
        h[3] = (Term)(STR, 0);

        var result = vm.ReadHeapTerm(3);

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
        var kb = Consult(MODULE);
        var vm = new ErgoVM();

        kb.RegisterBuiltInLabel("write", 1, (ErgoVM.__op)((vm) => {
            var term = (Term)vm.A[0];
            var text = vm.Pretty(term);
            Trace.WriteLine($"WRITE: {text}");
        }));

        var query = CompileQuery(kb, "parent(X, mary), write(X)");
        vm.Run(query);
    }

    [Fact]
    public void WriteBuiltIn_OutputsCorrectValue()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var output = new List<string>();

        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)((vm) => {
            var raw = vm.A[0];
            var term = (Term)raw;
            var addr = term.Tag == Term.__TAG.REF ? vm.deref(term.Value) : -1;
            var resolved = addr >= 0 ? (Term)vm.Store[addr] : term;
            Trace.WriteLine($"DIAG: raw={raw} tag={term.Tag} val={term.Value} deref={addr} resolved_tag={resolved.Tag} resolved_val={resolved.Value}");
            output.Add(vm.Pretty(term));
        }));

        var query = CompileQuery(kb, "parent(X, mary), my_write(X)");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(query);

        Assert.True(output.Count > 0, "Builtin never fired");
        Assert.Equal("john", output[0]);
    }

    [Fact]
    public void WriteBuiltIn_BacktracksCorrectly()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var output = new List<string>();

        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)((vm) => {
            var term = (Term)vm.A[0];
            output.Add(vm.Pretty(term));
        }));

        var query = CompileQuery(kb, "parent(X, Y), my_write(X)");
        vm.Run(query);

        Assert.Equal(2, output.Count);
        Assert.Equal("john", output[0]);
        Assert.Equal("mary", output[1]);
    }

    [Fact]
    public void FailingBuiltIn_CausesBacktrack()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();

        kb.RegisterBuiltInLabel("always_fail", 0, (ErgoVM.__op)((vm) => {
            vm.fail = true;
        }));

        var query = CompileQuery(kb, "fact, always_fail");
        var solutions = 0;
        vm.SolutionEmitted += _ => solutions++;
        vm.Run(query);

        Assert.Equal(0, solutions);
    }
}


