using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Runtime.WAM;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term.__TAG;
using Signature = Ergo.Compiler.Emission.Signature;
using Term = Ergo.Compiler.Emission.Term;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.UnitTests;

public class BuiltInTests : Tests
{
    private const string MODULE = "emitter_tests";

    #region Abstract Term Reconstruction
    [Fact]
    public void ReadHeapTerm_ReconstructsClauseWithConjunctionBody()
    {
        var constants = new Atom[] {
            (__string)"foo", (__string)"bar", (__string)"baz",
            (__string)",", (__string)":-"
        };
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        vm._QUERY = new Query(QueryBytecode.Preloaded([], constants), [], kb);
        var h = vm.Heap;
        h[0] = (Signature)(0, 1); h[1] = (Term)(REF, 1);
        h[2] = (Signature)(1, 1); h[3] = (Term)(REF, 1);
        h[4] = (Signature)(2, 1); h[5] = (Term)(REF, 1);
        h[6] = (Signature)(3, 2); h[7] = (Term)(STR, 2); h[8] = (Term)(STR, 4);
        h[9] = (Signature)(4, 2); h[10] = (Term)(STR, 0); h[11] = (Term)(STR, 6);
        h[12] = (Term)(STR, 9);
        var result = vm.ReadHeapTerm(12);
        Assert.IsType<Clause>(result);
        var clause = (Clause)result;
        Assert.IsType<Complex>(clause.Functor);
        Assert.Equal("foo", (string)((Complex)clause.Functor).Functor.Value);
        var goals = clause.Goals.ToArray();
        Assert.Equal(2, goals.Length);
        Assert.Equal("bar", (string)((Complex)goals[0]).Functor.Value);
        Assert.Equal("baz", (string)((Complex)goals[1]).Functor.Value);
        Assert.Equal(((Variable)((Complex)clause.Functor).Args[0]).Name, ((Variable)((Complex)goals[0]).Args[0]).Name);
        Assert.Equal(((Variable)((Complex)clause.Functor).Args[0]).Name, ((Variable)((Complex)goals[1]).Args[0]).Name);
    }

    [Fact]
    public void ReadHeapTerm_ReconstructsGroundFact()
    {
        var constants = new Atom[] { (__string)"parent", (__string)"john", (__string)"mary" };
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        vm._QUERY = new Query(QueryBytecode.Preloaded([], constants), [], kb);
        var h = vm.Heap;
        h[0] = (Signature)(0, 2); h[1] = (Term)(CON, 1); h[2] = (Term)(CON, 2); h[3] = (Term)(STR, 0);
        var result = vm.ReadHeapTerm(3);
        Assert.IsType<Complex>(result);
        var c = (Complex)result;
        Assert.Equal("parent", (string)c.Functor.Value);
        Assert.Equal("john", (string)((Atom)c.Args[0]).Value);
        Assert.Equal("mary", (string)((Atom)c.Args[1]).Value);
    }
    #endregion

    [Fact]
    public void WriteBuiltIn_Prints()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        kb.RegisterBuiltInLabel("write", 1, (ErgoVM.__op)(vm => Trace.WriteLine($"WRITE: {vm.Pretty(vm.A[0])}")));
        var q = CompileQuery(kb, "parent(X, mary), write(X)");
        vm.open_query(q);
        vm.next_solution();
        vm.close_query();
    }

    [Fact]
    public void WriteBuiltIn_OutputsCorrectValue()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var output = new List<string>();
        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)(vm => output.Add(vm.Pretty(vm.A[0]))));
        vm.findall(CompileQuery(kb, "parent(X, mary), my_write(X)"));
        Assert.True(output.Count > 0, "Builtin never fired");
        Assert.Equal("john", output[0]);
    }

    [Fact]
    public void WriteBuiltIn_BacktracksCorrectly()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var output = new List<string>();
        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)(vm => output.Add(vm.Pretty(vm.A[0]))));
        vm.findall(CompileQuery(kb, "parent(X, Y), my_write(X)"));
        Assert.Equal(2, output.Count);
        Assert.Equal("john", output[0]);
        Assert.Equal("mary", output[1]);
    }

    #region call/N
    [Fact]
    public void Call1_Diagnostic()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        var output = new List<string>();
        kb.RegisterBuiltInLabel("my_write", 1, (ErgoVM.__op)(vm => output.Add(vm.Pretty(vm.A[0]))));
        vm.findall(CompileQuery(kb, "call(parent(john, X)), my_write(X)"));
        Assert.Single(output);
        Assert.Equal("mary", output[0]);
    }

    [Fact]
    public void Call1_CallsPredicateFromHeapTerm()
        => AssertSolutions(new ErgoVM().findall(CompileQuery(Consult(MODULE), "call(parent(john, X))")), ["X/mary"]);

    [Fact]
    public void Call2_AppendsExtraArg()
        => AssertSolutions(new ErgoVM().findall(CompileQuery(Consult(MODULE), "call(parent(john), X)")), ["X/mary"]);

    [Fact]
    public void Call1_AtomCallWithExtraArgs()
        => Assert.Single(new ErgoVM().findall(CompileQuery(Consult(MODULE), "call(fact)")));

    [Fact]
    public void Call1_BacktracksOverMultipleSolutions()
        => AssertSolutions(new ErgoVM().findall(CompileQuery(Consult(MODULE), "call(parent(X, Y))")), ["X/john, Y/mary", "X/mary, Y/susan"]);
    #endregion

    [Fact]
    public void FailingBuiltIn_CausesBacktrack()
    {
        var kb = Consult(MODULE);
        var vm = new ErgoVM();
        kb.RegisterBuiltInLabel("always_fail", 0, (ErgoVM.__op)(vm => vm.fail = true));
        Assert.Empty(vm.findall(CompileQuery(kb, "fact, always_fail")));
    }
}
