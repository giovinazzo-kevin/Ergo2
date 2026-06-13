using Ergo.Compiler.Analysis;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Shared.Extensions;

namespace Ergo.UnitTests;

public class AnalyzerTests
{
    protected Module Load<T>()
        where T : Library
    {
        var moduleLocator = ModuleLocator.Default;
        var libraryLocator = new LibraryLocator(Libraries.Standard);
        var operatorLookup = new OperatorLookup();
        var analyzer = new Analyzer(moduleLocator, libraryLocator, operatorLookup);
        var moduleName = typeof(T).ToLibraryName();
        var graph = analyzer.LoadModule(moduleName);
        Assert.NotNull(graph);
        graph.Modules.TryGetValue(moduleName, out var module);
        Assert.NotNull(module);
        Assert.Equal(Module.Stage.Loaded, module.LoadStage);
        return module;
    }

    [Fact]
    public void Prologue()
    {
        var module = Load<Libs.Prologue.Lib.Prologue>();
        Assert.Equal(7, module.Predicates.Count);
        Assert.True(module.Predicates.TryGetValue(new("=", 2), out var unif_2));
        Assert.Equal(1, unif_2.Clauses.Count);
        Assert.True(module.Predicates.TryGetValue(new("->", 2), out var if_2));
        Assert.Equal(1, if_2.Clauses.Count);
        Assert.Equal(3, if_2.Clauses[0].Goals.Count);
        Assert.True(module.Predicates.TryGetValue(new(";", 2), out var or_2));
        Assert.Equal(4, or_2.Clauses.Count);
        // Builtins from library
        Assert.True(module.Predicates.ContainsKey(new("assert", 1)));
        Assert.True(module.Predicates.ContainsKey(new("assertz", 1)));
        Assert.True(module.Predicates.ContainsKey(new("asserta", 1)));
        Assert.True(module.Predicates.ContainsKey(new("retract", 1)));
    }

    [Fact]
    public void Math()
    {
        var module = Load<Libs.Math.Lib.Math>();
    }
}
