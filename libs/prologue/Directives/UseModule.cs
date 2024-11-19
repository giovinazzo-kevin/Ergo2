using Ergo.Compiler.Analysis;
using Ergo.Compiler.Analysis.Exceptions;
using Ergo.Lang.Ast;
namespace Ergo.Libs.Prologue.Directives;

public sealed class UseModule(Library parent) : Compiler.Analysis.Directive(parent, new("use_module", 1), 0)
{
    public override void Execute(Compiler.Analysis.Module module, ReadOnlySpan<Term> args)
    {
        var imports = args[0] switch
        {
            __string s => [s],
            List l => l.Contents,
            _ => throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(__string), Signature, args[0])
        };
        foreach (var imp in imports)
        {
            if (imp is not __string s)
                throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(__string), Signature, imp);
            if (module.Parent.Modules.TryGetValue(s, out var importedModule))
                module.Imports.Add(importedModule);
            else
                module.Imports.Add(module.Parent.Analyzer.LoadModule(module.Parent, s));
        }
    }
}