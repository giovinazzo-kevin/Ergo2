using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Libs;
using Ergo.Shared.Types;

namespace Ergo.Pipelines;

public class Consult : IPipeline<SourceInput, KnowledgeBase, Consult.Env>
{
    public class Env
    {
        public string BinPath { get; set; } = "./bin/";
        public ModuleLocator ModuleLocator { get; set; } = ModuleLocator.Default;
        public LibraryLocator LibraryLocator { get; set; } = new(Libraries.Standard);
    }

    internal static readonly Consult Instance = new();
    private Consult() { }

    public Result<KnowledgeBase, PipelineError> Run(SourceInput input, Env env)
    {
        if (input is not Case<__string> { Value: var name })
            return new PipelineError(this, new InvalidOperationException("Consult requires a module name"));

        var binDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, env.BinPath);
        var kbFile = Path.Combine(binDir, name + KnowledgeBaseLocator.EXT);
        var hashFile = kbFile + ".hash";

        if (!File.Exists(kbFile) || !File.Exists(hashFile))
            return new PipelineError(this, new FileNotFoundException(kbFile));

        env.ModuleLocator.Index.Update();
        var sourceFile = env.ModuleLocator.Index.Find(name).FirstOrDefault();
        if (sourceFile != null) {
            using var stream = sourceFile.OpenRead();
            var sourceBytes = System.Security.Cryptography.SHA256.HashData(stream);
            var versionBytes = BitConverter.GetBytes(Ergo.Compiler.Emission.BytecodeVersion.VERSION);
            var combined = new byte[sourceBytes.Length + versionBytes.Length];
            sourceBytes.CopyTo(combined, 0);
            versionBytes.CopyTo(combined, sourceBytes.Length);
            var currentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(combined));
            var cachedHash = File.ReadAllText(hashFile);
            if (currentHash != cachedHash)
                return new PipelineError(this, new InvalidOperationException("Source changed"));
        }

        var bytes = File.ReadAllBytes(kbFile);
        var words = new int[bytes.Length / sizeof(int)];
        Buffer.BlockCopy(bytes, 0, words, 0, bytes.Length);
        var kb = new KnowledgeBase(name, new KnowledgeBaseBytecode(words));

        // Re-link builtins and abstract terms from serialized imports
        var libs = kb.Bytecode.Imports
            .SelectMany(env.LibraryLocator.Find)
            .Select(t => Activator.CreateInstance(t, new Ergo.Compiler.Analysis.Module(new CallGraph(null!, name), name)))
            .OfType<Library>()
            .ToArray();
        foreach (var bi in libs.SelectMany(lib => lib.ExportedBuiltIns))
            kb.RegisterBuiltInLabel((string)bi.Signature.Functor.Value, bi.Signature.Arity, bi.Handler);
        foreach (var abs in libs.SelectMany(lib => lib.ExportedAbstractTerms))
            kb.RegisterAbstractTerm(abs);
        Compiler.Compile.BuildReconstructors(kb);

        return kb;
    }
}
