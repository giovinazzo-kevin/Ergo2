using Ergo.Compiler.Analysis;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;
using Ergo.Libs;
using Ergo.Pipelines.Compiler;
using Ergo.Shared.Types;
using System.Reflection;

namespace Ergo.Pipelines.Analysis;

public class Analyze : IPipeline<ErgoFileStream, CallGraph, Analyze.Env>
{
    public class Env
    {
        public ModuleLocator ModuleLocator { get; set; } = ModuleLocator.Default;
        public LibraryLocator LibraryLocator { get; set; } = new (Libraries.Standard);
        public OperatorLookup Operators { get; set; } = new ();
        public string DefaultImport { get; set; } = "prologue";
    }

    public static readonly Analyze Instance = new();
    private Analyze() { }

    public Result<CallGraph, PipelineError> Run(ErgoFileStream input, Env env)
    {
        var analyzer = new Analyzer(env.ModuleLocator, env.LibraryLocator, env.Operators, env.DefaultImport);
        return analyzer.LoadModule(input);
    }
}
