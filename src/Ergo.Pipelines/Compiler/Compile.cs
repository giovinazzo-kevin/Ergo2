using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Types;
using System.Security.Cryptography;
using static Ergo.Lang.Ast.Operator;
using AstTerm = Ergo.Lang.Ast.Term;

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
        BuildReconstructors(kb);
        if (env.SaveToPath is not null) {
            var binDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, env.SaveToPath);
            kb.Bytecode.SaveTo(new(Path.Combine(binDir, input.Root + ".kb")));
            env.ModuleLocator.Index.Update();
            var sourceFile = env.ModuleLocator.Index.Find(input.Root).FirstOrDefault();
            if (sourceFile != null) {
                using var stream = sourceFile.OpenRead();
                var sourceBytes = SHA256.HashData(stream);
                var versionBytes = BitConverter.GetBytes(BytecodeVersion.VERSION);
                var libBytes = System.Text.Encoding.UTF8.GetBytes(
                    string.Join("|", Ergo.Libs.Libraries.Standard.Select(a => a.FullName)));
                var combined = new byte[sourceBytes.Length + versionBytes.Length + libBytes.Length];
                sourceBytes.CopyTo(combined, 0);
                versionBytes.CopyTo(combined, sourceBytes.Length);
                libBytes.CopyTo(combined, sourceBytes.Length + versionBytes.Length);
                var hash = Convert.ToHexString(SHA256.HashData(combined));
                using var hashStream = new FileStream(
                    Path.Combine(binDir, input.Root + ".kb.hash"),
                    FileMode.Create, FileAccess.Write, FileShare.Read);
                using var hashWriter = new StreamWriter(hashStream);
                hashWriter.Write(hash);
            }
        }
        return kb;
    }

    internal static void BuildReconstructors(KnowledgeBase kb)
    {
        // Special reconstruction rules first (TryAdd won't overwrite)
        kb.Reconstructors.TryAdd((Operators.HornBinary.Functors.First().Value, 2),
            args => new Lang.Ast.Clause(args[0], args[1]));
        kb.Reconstructors.TryAdd((Operators.HornUnary.Functors.First().Value, 1),
            args => new Lang.Ast.Directive(args[0]));
        // Generic operator reconstruction
        foreach (var op in kb.Bytecode.Operators.Values) {
            int arity = op.Fixity_ switch {
                Fixity.Prefix or Fixity.Postfix => 1,
                Fixity.Infix => 2,
                _ => throw new NotSupportedException()
            };
            Func<AstTerm[], AstTerm> factory = op.Fixity_ switch {
                Fixity.Infix => args => new BinaryExpression(op, args[0], args[1]),
                Fixity.Prefix => args => new PrefixExpression(op, args[0]),
                Fixity.Postfix => args => new PostfixExpression(op, args[0]),
                _ => throw new NotSupportedException()
            };
            foreach (Atom functor in op.Functors)
                kb.Reconstructors.TryAdd((functor.Value, arity), factory);
        }
    }
}