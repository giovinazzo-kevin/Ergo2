using Ergo.Language.Ast;
using Ergo.Language.Lexing;
using Ergo.Shared.Types;

namespace Ergo.Compiler.Analysis;

public class CallGraph(Analyzer analyzer)
{
    public abstract class Node<TParent>
    {
        public abstract TParent Parent { get; }
    }

    public readonly Analyzer Analyzer = analyzer;
    public readonly Dictionary<__string, Module> Modules = [];

    public IEnumerable<Directive> ResolveDirectives(Signature signature, Module context)
    {
        if (signature.Module.TryGetValue(out var qualification)
            && Modules.TryGetValue(qualification, out var module))
        {
            return module.Libraries
                .SelectMany(l => l.ExportedDirectives
                    .Where(d => d.Signature == signature.Unqualified));
        }
        return context.Libraries
                .SelectMany(l => l.ExportedDirectives)
                .Where(d => d.Signature == signature.Unqualified)
            .Concat(context.Imports
                .SelectMany(i => ResolveDirectives(signature, i)));
    }

}
