using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Runtime.WAM;
using System.ComponentModel;
using System.Diagnostics;
using static Ergo.Compiler.Emission.Term;
using static Ergo.Compiler.Emission.Term.__TAG;

namespace Ergo.UnitTests;

public class VMTests : Tests
{


    [Theory]
    [InlineData(CON, 2852519)]
    [InlineData(CON, -1)]
    [InlineData(REF, 3)]
    [InlineData(STR, int.MaxValue)]
    [InlineData(STR, -348534)]
    public void WordsArePackedCorrectly(__TAG tag, int value)
    {
        Term fromValue = (tag, value);
        Term fromRawValue = fromValue.RawValue;
        Assert.Equal(fromValue.RawValue, fromRawValue.RawValue);
        Assert.Equal(fromValue.Value, fromRawValue.Value);
        Assert.Equal(fromValue.Tag, fromRawValue.Tag);
    }


    [Fact]
    public void Signature_Should_Pack_And_Unpack_Correctly()
    {
        var packed = (Signature)(f: 42, n: 3);
        Assert.Equal(42, packed.F);
        Assert.Equal(3, packed.N);
    }


    [Theory]
    [InlineData("fact", 1)]
    [InlineData("another_fact", 1)]
    [InlineData("multiple_fact", 2)]
    public void FactSucceeds(string fact, int numSolutions)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var query = kb.Query(fact);
        var vm = new ErgoVM();
        var actualSolutions = 0;
        vm.SolutionEmitted += (_) => actualSolutions++;
        vm.Run(query);
        Assert.Equal(numSolutions, actualSolutions);
    }

    [Theory]
    [InlineData("parent(john, mary)", 1)]
    [InlineData("parent(mary, susan)", 1)]
    [InlineData("parent(susan, john)", 0)]
    public void ParentFactsWork(string query, int expected)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var q = kb.Query(query);
        var vm = new ErgoVM();
        var actual = 0;
        vm.SolutionEmitted += (_) => actual++;
        vm.Run(q);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("parent(susan, john)", new string[] { })]
    [InlineData("parent(X, mary)", new string[] { "X/john" })]
    [InlineData("parent(mary, X)", new string[] { "X/susan" })]
    [InlineData("parent(X, Y)", new string[] { "X/john, Y/mary", "X/mary, Y/susan", "X/susan, Y/john" })]
    public void ParentQueryWorks(string query, string[] subs)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var q = kb.Query(query);
        var vm = new ErgoVM();
        var actual = 0;
        vm.SolutionEmitted += (_) =>
        {
            Assert.Equal(subs[actual++], vm.MaterializeSolution().ToString());
        };
        vm.Run(q);
        Assert.Equal(subs.Length, actual);
    }

    [Theory]
    [InlineData("ancestor(john, mary)", 1)]
    [InlineData("ancestor(john, susan)", 1)]
    [InlineData("parent(X, Y), !, complex_fact(0,1,2).", 1)]
    public void AncestorSucceeds(string query, int expected)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var q = kb.Query(query);
        var vm = new ErgoVM();
        var solutions = 0;
        vm.SolutionEmitted += (_) =>
        {
            Trace.WriteLine(vm.MaterializeSolution());
            solutions++;
        };
        vm.Run(q);
        Assert.Equal(expected, solutions);
    }

    [Theory]
    [InlineData("complex_fact(A, B, C)", 1)]
    public void BindingSucceeds(string fact, int numSolutions)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var query = kb.Query(fact);
        var vm = new ErgoVM();
        var actualSolutions = 0;
        vm.SolutionEmitted += (_) => actualSolutions++;
        vm.Run(query);
        Assert.Equal(numSolutions, actualSolutions);
    }

    [Fact]
    public void QueryEmitsPutVariableCorrectly()
    {
        AssertQuery("parent(X, Y)", Validate);

        void Validate(QueryBytecode bytes)
        {
            var span = bytes.Query;
            AssertOp(OpCode.allocate, ref span);
            AssertOp(OpCode.put_variable, ref span);
            AssertInt32(0, ref span);
            AssertInt32(0, ref span);
            AssertOp(OpCode.put_variable, ref span);
            AssertInt32(1, ref span);
            AssertInt32(1, ref span);
            AssertOp(OpCode.call, ref span);
            AssertSignature("parent", 2, ref span, bytes);
        }
    }


    [Fact]
    public void RefDerefsToConstant()
    {
        var vm = new ErgoVM();
        var addr = 1033; // manually chosen RAM slot
        vm.Store[addr] = (Term)(REF, addr);  // reflexive REF
        vm.Store[addr] = (Term)(CON, 4);     // now overwrite it with a constant

        var deref = vm.deref(addr);
        Assert.Equal(addr, deref); // should resolve to same addr
        Assert.Equal((int)(Term)(CON, 4), (int)(Term)vm.Store[deref]);
    }

    [Fact]
    public void PutVariableWritesStackCorrectly()
    {
        var vm = new ErgoVM();
        vm.E = 0; // Frame pointer at start of stack

        // Simulate instruction: PutVariable Y0, A0
        vm._QUERY = QueryBytecode.Preloaded([0, 0]);

        vm.PutVariable();

        var stackAddr = (0 + 0 + 1);
        var term = (Term)vm.Stack[stackAddr];
        Assert.Equal(stackAddr, term.Value);
        Assert.Equal((int)term, vm.A[0]);
    }

    [Fact]
    public void UndefinedPredicateThrows()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        Assert.Throws<InvalidOperationException>(() => kb.Query("undefined_predicate"));
    }

    [Fact]
    public void Unify_TwoUnboundRefs_BindsThemTogether()
    {
        var vm = new ErgoVM();
        var a1 = 100;
        var a2 = 104;
        vm.Store[a1] = (Term)(REF, a1);
        vm.Store[a2] = (Term)(REF, a2);

        vm.unify(a1, a2);

        var deref1 = vm.deref(a1);
        var deref2 = vm.deref(a2);
        Assert.Equal(deref1, deref2);
    }

    [Fact]
    public void Unify_UnboundRefAndConstant_BindsRefToConst()
    {
        var vm = new ErgoVM();
        var a1 = 100;
        var a2 = 104;
        vm.Store[a1] = (Term)(REF, a1);
        vm.Store[a2] = (Term)(CON, 7); // let's say 7 is “john”

        vm.unify(a1, a2);

        Assert.Equal(((Term)(CON, 7)).RawValue, ((Term)vm.Store[vm.deref(a1)]).RawValue);
    }

    [Fact]
    public void Unify_TwoDifferentConstants_Fails()
    {
        var vm = new ErgoVM();
        var a1 = 100;
        var a2 = 104;
        vm.Store[a1] = (Term)(CON, 1);
        vm.Store[a2] = (Term)(CON, 2);

        vm.unify(a1, a2);

        Assert.True(vm.fail);
    }

    [Fact]
    public void Unify_SameFunctorAndArgs_Succeeds()
    {
        var vm = new ErgoVM();
        var f = (Term)(CON, 10); // "father/2" or whatever
        var h1 = 100;
        var h2 = 200;

        // STR layout: [STR, f, arg1, arg2]
        vm.Store[h1] = (Term)(STR, h1 + 1);
        vm.Store[h1 + 1] = f;
        vm.Store[h1 + 2] = (Term)(CON, 1);
        vm.Store[h1 + 3] = (Term)(CON, 2);

        vm.Store[h2] = (Term)(STR, h2 + 1);
        vm.Store[h2 + 1] = f;
        vm.Store[h2 + 2] = (Term)(CON, 1);
        vm.Store[h2 + 3] = (Term)(CON, 2);

        vm.unify(h1, h2);

        Assert.False(vm.fail);
    }

    [Fact]
    public void Unify_DifferentFunctorAndArgs_Fails()
    {
        var vm = new ErgoVM();
        var f1 = (Term)(CON, 10);
        var f2 = (Term)(CON, 11);
        var h1 = 100;
        var h2 = 200;

        // STR layout: [STR, f, arg1, arg2]
        vm.Store[h1] = (Term)(STR, h1 + 1);
        vm.Store[h1 + 1] = f1;
        vm.Store[h1 + 2] = (Term)(CON, 1);
        vm.Store[h1 + 3] = (Term)(CON, 2);

        vm.Store[h2] = (Term)(STR, h2 + 1);
        vm.Store[h2 + 1] = f2;
        vm.Store[h2 + 2] = (Term)(CON, 1);
        vm.Store[h2 + 3] = (Term)(CON, 2);

        vm.unify(h1, h2);

        Assert.True(vm.fail);
    }
}
