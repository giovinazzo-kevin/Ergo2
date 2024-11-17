using Ergo.Compiler.Analysis;
using Ergo.Compiler.Analysis.Exceptions;
using Ergo.Language.Ast;
using Ergo.Language.Ast.Extensions;
using Ergo.Language.Ast.WellKnown;
using System.ComponentModel;
namespace Ergo.Libs.Prologue.Directives;

public sealed class DeclareModule(Library parent) : Compiler.Analysis.Directive(parent, new("module", 2), -1)
{
    public override void Execute(Compiler.Analysis.Module module, ReadOnlySpan<Term> args)
    {
        module.Name = args[0] switch
        {
            __string s when s != module.Name => throw new AnalyzerException(AnalyzerError.Module0MustBeNamed1, s, module.Name),
            __string s => s,
            Variable { Bound: false } v => (__string)(v.Value = module.Name),
            _ => throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(__string), Signature, args[0])
        };
        var exports = args[1] switch
        {
            Atom a when a == Literals.EmptyList => [],
            List l => l.Contents,
            _ => throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(List), Signature, args[1])
        };
        foreach (var exp in exports)
        {
            if (exp is not SignatureExpression { Functor: var functor, Arity: var arity })
                throw new AnalyzerException(AnalyzerError.ExpectedTermOfType0At1Found2, typeof(SignatureExpression), Signature, exp);
            module.Exports.Add(new(default, functor, arity));
        }
    }
}
