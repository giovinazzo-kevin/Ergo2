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

public class VMTests
{
    protected KnowledgeBase Consult(string moduleName)
    {
        const string MODULE_PATH = "./ergo/";
        const string BIN_PATH = "./bin/";
        var kbLocator = new KnowledgeBaseLocator(BIN_PATH);
        var compiledKb = kbLocator.Index.Find(moduleName).FirstOrDefault();
        if (compiledKb != null)
            return new KnowledgeBase(ErgoFileStream.Open(compiledKb));
        var moduleLocator = new ModuleLocator(MODULE_PATH);
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        var graph = analyzer.LoadModule(moduleName);
        var emitter = new Emitter();
        var kb = emitter.KnowledgeBase(graph);
        return kb;
    }

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
        vm.Solution += _ => actualSolutions++;
        vm.Run(query);
        Assert.Equal(numSolutions, actualSolutions);
    }

    [Theory]
    [InlineData("complex_fact(A, B, C)", 1)]
    public void BindingSucceeds(string fact, int numSolutions)
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var query = kb.Query(fact);
        var vm = new ErgoVM();
        var actualSolutions = 0;
        vm.Solution += _ => actualSolutions++;
        vm.Run(query);
        Assert.Equal(numSolutions, actualSolutions);
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

        var stackAddr = ErgoVM.__STACK(0 + 0 + 1);
        var term = (Term)vm.Store[stackAddr];

        Assert.True(term.Tag == REF);
        Assert.Equal(stackAddr, term.Value);
        Assert.Equal((int)term, vm.A[0]);
    }

    [Fact]
    public void GetConstantBindsRefToConstant()
    {
        var vm = new ErgoVM();
        var addr = 1033;

        vm._QUERY = QueryBytecode.Preloaded([(int)(Term)(CON, 0), 0], ["hello"]);
        vm.Store[addr] = (Term)(REF, addr);
        vm.A[0] = addr;

        vm.GetConstant();
        Trace.WriteLine($"Expected: {(int)(Term)(CON, 0)}");
        Trace.WriteLine($"Actual: {(int)(Term)vm.Store[addr]}");

        Assert.Equal((int)(Term)(CON, 0), vm.Store[addr]);
    }

    [Fact]
    public void UndefinedPredicateThrows()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        Assert.Throws<InvalidOperationException>(() => kb.Query("undefined_predicate"));
    }
}
