using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Runtime.WAM;
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
    public void FactSucceeds()
    {
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var query = kb.Query("fact");
        var vm = new ErgoVM();
        vm.Run(query);
    }
}
