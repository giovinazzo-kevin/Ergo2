using Ergo.Compiler.Emission;
using Ergo.Runtime.WAM;
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

    // All query-level tests: rows are cases, one method is the invariant
    [Theory]
    // ground facts — no bindings
    [InlineData("emitter_tests", "fact", new string[] { "" })]
    [InlineData("emitter_tests", "another_fact", new string[] { "" })]
    [InlineData("emitter_tests", "multiple_fact", new string[] { "", "" })]
    [InlineData("emitter_tests", "complex_fact(0, 1, 2)", new string[] { "" })]
    [InlineData("emitter_tests", "parent(john, mary)", new string[] { "" })]
    [InlineData("emitter_tests", "parent(mary, susan)", new string[] { "" })]
    [InlineData("emitter_tests", "parent(susan, john)", new string[] { })]
    // fact queries with bindings
    [InlineData("emitter_tests", "parent(X, mary)", new string[] { "X/john" })]
    [InlineData("emitter_tests", "parent(mary, X)", new string[] { "X/susan" })]
    [InlineData("emitter_tests", "parent(X, Y)", new string[] { "X/john, Y/mary", "X/mary, Y/susan" })]
    [InlineData("emitter_tests", "complex_fact(A, B, C)", new string[] { "A/0, B/1, C/2" })]
    // backtracking
    [InlineData("backtrack_tests", "value(X)", new string[] { "X/1", "X/2", "X/3" })]
    // rules with body goals — variable export
    [InlineData("emitter_tests", "ancestor(john, mary)", new string[] { "" })]
    [InlineData("emitter_tests", "ancestor(john, susan)", new string[] { "" })]
    [InlineData("emitter_tests", "ancestor(X, Y)", new string[] { "X/john, Y/mary" })]
    [InlineData("emitter_tests", "ancestor(john, X)", new string[] { "X/mary" })]
    [InlineData("emitter_tests", "ancestor(X, susan)", new string[] { "X/mary" })]
    // rules with body conjunction — no cut — variable export
    [InlineData("emitter_tests", "mutual(a, b)", new string[] { "" })]
    [InlineData("emitter_tests", "mutual(a, X)", new string[] { "X/b" })]
    [InlineData("emitter_tests", "mutual(X, Y)", new string[] { "X/a, Y/b", "X/b, Y/a" })]
    // conjunction with cut
    [InlineData("emitter_tests", "parent(X, Y), !, complex_fact(0,1,2).", new string[] { "X/john, Y/mary" })]
    public void Query(string module, string query, string[] expected)
    {
        var kb = Consult(module);
        var q = kb.Query(query);
        var vm = new ErgoVM();
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += v => solutions.Add(v.MaterializeSolution());
        vm.Run(q);
        AssertSolutions(solutions, expected);
    }

    [Fact]
    public void UndefinedPredicateThrows()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        Assert.Throws<InvalidOperationException>(() => kb.Query("undefined_predicate"));
    }

    [Fact]
    public void MutualQueryEmitsCorrectly()
    {
        AssertQuery(nameof(EmitterTests.emitter_tests), "mutual(a, X)", Validate);

        void Validate(QueryBytecode bytes)
        {
            var span = bytes.Query;
            AssertOp(OpCode.allocate, ref span);
            AssertOp(OpCode.put_constant, ref span);
            AssertConst("a", ref span, bytes);
            AssertInt32(0, ref span);
            AssertOp(OpCode.put_variable, ref span);
            AssertInt32(0, ref span); // V0
            AssertInt32(1, ref span); // A1
            AssertOp(OpCode.call, ref span);
            AssertSignature("mutual", 2, ref span, bytes);
            AssertOp(OpCode.put_unsafe_value, ref span);
            AssertInt32(0, ref span); // V0
            AssertInt32(1, ref span); // A1
            AssertOp(OpCode.deallocate, ref span);
            AssertOp(OpCode.halt, ref span);
        }
    }

    #region Bytecode emission
    [Fact]
    public void QueryEmitsPutVariableCorrectly()
    {
        AssertQuery(nameof(EmitterTests.emitter_tests), "parent(X, Y)", Validate);

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
    public void QueryEmitsParseCorrectly()
    {
        AssertQuery("vm_tests", "parse(Tree)", Validate);

        void Validate(QueryBytecode bytes)
        {
            var span = bytes.Query;
            AssertOp(OpCode.allocate, ref span);
            AssertOp(OpCode.put_variable, ref span);
            AssertInt32(0, ref span);
            AssertInt32(0, ref span);
            AssertOp(OpCode.call, ref span);
            AssertSignature("parse", 1, ref span, bytes);
            AssertOp(OpCode.put_unsafe_value, ref span);
            AssertInt32(0, ref span);
            AssertInt32(0, ref span);
            AssertOp(OpCode.deallocate, ref span);
        }
    }

    [Fact]
    public void BacktrackEmitsCorrectly()
    {
        AssertQuery("backtrack_tests", "value(X)", Validate);

        void Validate(QueryBytecode bytes)
        {
            var span = bytes.Query;
            AssertOp(OpCode.allocate, ref span);
            AssertOp(OpCode.put_variable, ref span);
            AssertInt32(0, ref span);
            AssertInt32(0, ref span);
            AssertOp(OpCode.call, ref span);
            AssertSignature("value", 1, ref span, bytes);
            AssertOp(OpCode.put_unsafe_value, ref span);
            AssertInt32(0, ref span);
            AssertInt32(0, ref span);
            AssertOp(OpCode.deallocate, ref span);
        }
    }

    [Fact]
    public void BlackBookPredicatesEmitCorrectly()
    {
        AssertQuery("wam_tests", "p1(X, Y, Z)", Validate);

        void Validate(QueryBytecode bytes)
        {
            var span = bytes.Query;
            AssertOp(OpCode.allocate, ref span);
            AssertOp(OpCode.put_variable, ref span);
        }
    }
    #endregion

    #region VM internals
    [Fact]
    public void RefDerefsToConstant()
    {
        var vm = new ErgoVM();
        var addr = 1033;
        vm.Store[addr] = (Term)(REF, addr);
        vm.Store[addr] = (Term)(CON, 4);
        var deref = vm.deref(addr);
        Assert.Equal(addr, deref);
        Assert.Equal((int)(Term)(CON, 4), (int)(Term)vm.Store[deref]);
    }

    [Fact]
    public void PutVariableWritesStackCorrectly()
    {
        var vm = new ErgoVM();
        vm.E = ErgoVM.HEAP_SIZE;
        vm._QUERY = QueryBytecode.Preloaded([0, 0]);
        vm.PutVariable();
        var storeAddr = ErgoVM.HEAP_SIZE + 0 + 2;
        var term = (Term)vm.Store[storeAddr];
        Assert.Equal(storeAddr, term.Value);
        Assert.Equal((int)term, vm.A[0]);
    }

    [Fact]
    public void Unify_TwoUnboundRefs_BindsThemTogether()
    {
        var vm = new ErgoVM();
        vm.Store[100] = (Term)(REF, 100);
        vm.Store[104] = (Term)(REF, 104);
        vm.unify(100, 104);
        Assert.Equal(vm.deref(100), vm.deref(104));
    }

    [Fact]
    public void Unify_UnboundRefAndConstant_BindsRefToConst()
    {
        var vm = new ErgoVM();
        vm.Store[100] = (Term)(REF, 100);
        vm.Store[104] = (Term)(CON, 7);
        vm.unify(100, 104);
        Assert.Equal(((Term)(CON, 7)).RawValue, ((Term)vm.Store[vm.deref(100)]).RawValue);
    }

    [Fact]
    public void Unify_TwoDifferentConstants_Fails()
    {
        var vm = new ErgoVM();
        vm.Store[100] = (Term)(CON, 1);
        vm.Store[104] = (Term)(CON, 2);
        vm.unify(100, 104);
        Assert.True(vm.fail);
    }

    [Fact]
    public void Unify_SameFunctorAndArgs_Succeeds()
    {
        var vm = new ErgoVM();
        var f = (Term)(CON, 10);
        vm.Store[100] = (Term)(STR, 101);
        vm.Store[101] = f;
        vm.Store[102] = (Term)(CON, 1);
        vm.Store[103] = (Term)(CON, 2);
        vm.Store[200] = (Term)(STR, 201);
        vm.Store[201] = f;
        vm.Store[202] = (Term)(CON, 1);
        vm.Store[203] = (Term)(CON, 2);
        vm.unify(100, 200);
        Assert.False(vm.fail);
    }

    [Fact]
    public void Unify_DifferentFunctorAndArgs_Fails()
    {
        var vm = new ErgoVM();
        vm.Store[100] = (Term)(STR, 101);
        vm.Store[101] = (Term)(CON, 10);
        vm.Store[102] = (Term)(CON, 1);
        vm.Store[103] = (Term)(CON, 2);
        vm.Store[200] = (Term)(STR, 201);
        vm.Store[201] = (Term)(CON, 11);
        vm.Store[202] = (Term)(CON, 1);
        vm.Store[203] = (Term)(CON, 2);
        vm.unify(100, 200);
        Assert.True(vm.fail);
    }
    #endregion
}
