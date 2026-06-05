using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Pipelines.Analysis;
using Ergo.Pipelines.Compiler;
using Ergo.Pipelines.IO;
using Ergo.Shared.Types;
using System.Security.Cryptography;

namespace Ergo.Pipelines;

public class Consult : IPipeline<__string, KnowledgeBase, Consult.Env>
{
    public class Env
    {
        public LoadSource.Env LoadSource { get; set; } = new();
        public Analyze.Env Analyze { get; set; } = new();
        public Compile.Env Compile { get; set; } = new();
        public string BinPath { get; set; } = "./bin/";
    }

    internal static readonly Consult Instance = new();
    private Consult() { }

    public Result<KnowledgeBase, PipelineError> Run(__string moduleName, Env env)
    {
        var name = (string)moduleName;
        var binDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, env.BinPath);
        var kbFile = new FileInfo(Path.Combine(binDir, name + KnowledgeBaseLocator.EXT));
        var hashFile = new FileInfo(kbFile.FullName + ".hash");

        // Find source file for hash comparison
        env.LoadSource.ModuleLocator.Index.Update();
        var sourceFile = env.LoadSource.ModuleLocator.Index.Find(name).FirstOrDefault();

        // Check if compiled KB is fresh
        if (sourceFile != null && kbFile.Exists && hashFile.Exists)
        {
            var sourceHash = HashSource(sourceFile);
            var storedHash = File.ReadAllText(hashFile.FullName);
            if (sourceHash == storedHash)
                return new KnowledgeBase(ErgoFileStream.Open(kbFile));
        }

        // Run the full pipeline
        env.Compile.SaveToPath = binDir;
        var result = Pipeline
            .WithStep(Steps.LoadSource, env.LoadSource)
            .WithStep(Steps.Analyze, env.Analyze)
            .WithStep(Steps.Compile, env.Compile)
            .Run(moduleName);

        if (result is Success<KnowledgeBase> { Value: var kb } && sourceFile != null)
        {
            if (!Directory.Exists(binDir))
                Directory.CreateDirectory(binDir);
            File.WriteAllText(hashFile.FullName, HashSource(sourceFile));
        }

        return result;
    }

    static string HashSource(FileInfo file)
    {
        using var stream = file.OpenRead();
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
