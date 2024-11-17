using Ergo.IO;
using Ergo.Compiler.Analysis;
using Ergo.Language.Lexing;
using Ergo.Libs;
using Ergo.Libs.Math;
using Ergo.Libs.Prologue;
using Ergo.Shared.Extensions;

namespace Ergo.UnitTests;

public class AnalyzerTests
{
    protected Module Load<T>()
        where T : Library
    {
        var moduleLocator = new ModuleLocator("./ergo/");
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        var moduleName = typeof(T).ToLibraryName();
        var graph = analyzer.Load(moduleName);
        Assert.NotNull(graph);
        graph.Modules.TryGetValue(moduleName, out var module);
        Assert.NotNull(module);
        Assert.Equal(Module.Stage.Loaded, module.LoadStage);
        return module;
    }

    [Fact]
    public void Prologue()
    {
        var module = Load<LibPrologue>();
        Assert.True(module.Predicates.TryGetValue(new("->", 2), out var if_2));
        Assert.Equal(1, if_2.Clauses.Count);
        Assert.True(module.Predicates.TryGetValue(new(";", 2), out var or_2));
        Assert.Equal(4, or_2.Clauses.Count);
    }

    [Fact]
    public void Math()
    {
        var module = Load<LibMath>();
    }
}
