using Ergo.Compiler.Analysis;
using Ergo.Compiler.Analysis.Exceptions;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;

namespace Ergo.Libs.Stdlib.Directives;

public sealed class DeclareDynamic(Library parent) : Compiler.Analysis.Directive(parent, new("dynamic", 1), 20)
{
    public override void Execute(Compiler.Analysis.Module module, ReadOnlySpan<Term> args)
    {
        var sig = args[0].GetSignature()
            .GetOrThrow(new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(Signature), Signature, args[0]));
        module.Dynamic.Add(sig.Unqualified);
    }
}
