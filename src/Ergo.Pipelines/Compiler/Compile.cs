using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.Pipelines.IO;
using Ergo.Shared.Types;

namespace Ergo.Pipelines.Compiler;

public class Compile : IPipeline<CallGraph, KnowledgeBase, Compile.Env>
{
    public class Env
    {
        /// <summary>
        /// If set, also writes the knowledge base to disk at the given path. 
        /// Otherwise, the knowledge base is only kept in memory.
        /// </summary>
        public string? SaveToPath { get; set; } = null;
    }


    internal static readonly Compile Instance = new();
    private Compile() { }

    public Result<KnowledgeBase, PipelineError> Run(CallGraph input, Env env)
    {
        var emitter = new Emitter();
        var kb = emitter.Compile(input);
        if (env.SaveToPath is not null)
            kb.SaveTo(new(Path.Combine(env.SaveToPath, input.Root + ".kb")));
        return kb;
    }
}