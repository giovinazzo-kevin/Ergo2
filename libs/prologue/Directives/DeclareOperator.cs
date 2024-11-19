using Ergo.Compiler.Analysis;
using Ergo.Compiler.Analysis.Exceptions;
using Ergo.Lang.Ast;
namespace Ergo.Libs.Prologue.Directives;

public sealed class DeclareOperator(Library parent) : Compiler.Analysis.Directive(parent, new("op", 3), 10)
{
    public override void Execute(Compiler.Analysis.Module module, ReadOnlySpan<Term> args)
    {
        if (args[0] is not __int precedence)
            throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(__int), Signature, args[0]);
        if (args[1] is not __string strType || !Enum.TryParse<Operator.Type>(strType, out var type))
            throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, "Operator.Type", Signature, args[1]);
        var synonyms = args[2] switch {
            List l => l.Contents,
            _ => [args[2]]
        };
        foreach (var syn in synonyms.Where(x => x is not Atom))
            throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(Atom), Signature, syn);
        var lookup = module.Parent.Analyzer.Operators;
        lookup.AddRange(new Operator(precedence, type, [.. synonyms.Cast<Atom>()]));
    }
}
