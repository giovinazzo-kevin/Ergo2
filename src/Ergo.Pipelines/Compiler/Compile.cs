using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Shared.Types;
using System.Security.Cryptography;

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
        public ModuleLocator ModuleLocator { get; set; } = ModuleLocator.Default;
    }


    internal static readonly Compile Instance = new();
    private Compile() { }

    public Result<KnowledgeBase, PipelineError> Run(CallGraph input, Env env)
    {
        var emitter = new Emitter();
        var kb = emitter.KnowledgeBase(input);
        if (env.SaveToPath is not null) {
            var binDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, env.SaveToPath);
            kb.Bytecode.SaveTo(new(Path.Combine(binDir, input.Root + ".kb")));
            env.ModuleLocator.Index.Update();
            var sourceFile = env.ModuleLocator.Index.Find(input.Root).FirstOrDefault();
            if (sourceFile != null) {
                using var stream = sourceFile.OpenRead();
                var sourceBytes = SHA256.HashData(stream);
                var versionBytes = BitConverter.GetBytes(BytecodeVersion.VERSION);
                var combined = new byte[sourceBytes.Length + versionBytes.Length];
                sourceBytes.CopyTo(combined, 0);
                versionBytes.CopyTo(combined, sourceBytes.Length);
                var hash = Convert.ToHexString(SHA256.HashData(combined));
                File.WriteAllText(Path.Combine(binDir, input.Root + ".kb.hash"), hash);
            }
        }
        return kb;
    }
}