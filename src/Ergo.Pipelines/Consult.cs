using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Shared.Types;
using System.Security.Cryptography;

namespace Ergo.Pipelines;

public class Consult : IPipeline<KnowledgeBase, KnowledgeBase, Consult.Env>
{
    public class Env
    {
        public string BinPath { get; set; } = "./bin/";
        public ModuleLocator ModuleLocator { get; set; } = ModuleLocator.Default;
    }

    internal static readonly Consult Instance = new();
    private Consult() { }

    public Result<KnowledgeBase, PipelineError> Run(KnowledgeBase kb, Env env)
    {
        env.ModuleLocator.Index.Update();
        var sourceFile = env.ModuleLocator.Index.Find(kb.Name).FirstOrDefault();
        if (sourceFile == null)
            return kb;
        var binDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, env.BinPath);
        if (!Directory.Exists(binDir))
            Directory.CreateDirectory(binDir);
        var hashFile = Path.Combine(binDir, kb.Name + KnowledgeBaseLocator.EXT + ".hash");
        using var stream = sourceFile.OpenRead();
        File.WriteAllText(hashFile, Convert.ToHexString(SHA256.HashData(stream)));
        return kb;
    }
}
