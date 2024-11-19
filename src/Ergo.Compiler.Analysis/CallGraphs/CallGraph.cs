using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class CallGraph(Analyzer analyzer, __string rootModule)
{
    public abstract class Node<TParent>
    {
        public abstract TParent Parent { get; }
    }

    public readonly Analyzer Analyzer = analyzer;
    public readonly Dictionary<__string, Module> Modules = [];
    public readonly __string Root = rootModule;

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
