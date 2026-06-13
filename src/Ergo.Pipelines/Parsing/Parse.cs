using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;
using AstTerm = Ergo.Lang.Ast.Term;
using ParseDelegate = Ergo.Lang.Parsing.WellKnown.Delegates.Parse;

namespace Ergo.Pipelines.Parsing;

public class Parse : IPipeline<__string, AstTerm, Parse.Env>
{
    public class Env
    {
        public KnowledgeBase KB { get; init; } = null!;
    }

    internal static readonly Parse Instance = new();
    private Parse() { }

    public Result<AstTerm, PipelineError> Run(__string input, Env env)
    {
        var file = ErgoFileStream.Create((string)input.Value, "query");
        var lexer = new Lexer(file, env.KB.Bytecode.Operators);
        var parser = new Parser(lexer);
        foreach (var abs in env.KB.AbstractTerms.Values) {
            var factory = (ParseDelegate)abs.Parse;
            parser.AddAbstractParser(factory(parser));
        }
        if (!parser.BinaryExpressionRhs().TryGetValue(out var ast))
            return new PipelineError(this, new InvalidOperationException("Failed to parse query"));
        return ast;
    }
}
