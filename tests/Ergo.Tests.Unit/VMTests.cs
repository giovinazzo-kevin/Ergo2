using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Runtime.WAM;

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
            return KnowledgeBase.Deserialize(ErgoFileStream.Open(compiledKb));
        var moduleLocator = new ModuleLocator(MODULE_PATH);
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        var graph = analyzer.Load(moduleName);
        var kb = Emitter.KnowledgeBase(graph);
        var fs = KnowledgeBase.Serialize(kb);
        fs.Save(Path.Combine(BIN_PATH, kb.Name) + KnowledgeBaseLocator.EXT);
        return kb;
    }

    protected Query Query(KnowledgeBase kb, string query)
    {
        var toplevel = Analyzer.Parse(query, kb.Operators);
        return Emitter.Query(kb, toplevel);
    }

    [Fact]
    public void vm_tests()
    {
        var vm = new ErgoVM();
        var kb = Consult(nameof(EmitterTests.emitter_tests));
        var q = Query(kb, "fact");
        vm.Run(kb, q);
    }
}
