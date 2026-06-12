using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;
using ParseDelegate = Ergo.Lang.Parsing.WellKnown.Delegates.Parse;

namespace Ergo.Pipelines.Compiler;

public class CompileQuery : IPipeline<(KnowledgeBase KB, string Query), Query, CompileQuery.Env>
{
    public class Env { }

    internal static readonly CompileQuery Instance = new();
    private CompileQuery() { }

    public Result<Query, PipelineError> Run((KnowledgeBase KB, string Query) input, Env env)
    {
        var file = ErgoFileStream.Create(input.Query, nameof(CompileQuery));
        using var lexer = new Lexer(file, input.KB.Bytecode.Operators);
        using var parser = new Parser(lexer);
        foreach (var abs in input.KB.AbstractTerms.Values) {
            if (abs.Parse == null) continue;
            var factory = (ParseDelegate)abs.Parse;
            var production = factory(parser);
            if (production != null)
                parser.AddAbstractParser(production);
        }
        if (!parser.BinaryExpressionRhs().TryGetValue(out var ast))
            return new PipelineError(this, new InvalidOperationException("Failed to parse query"));
        var emitter = new Emitter();
        var q = emitter.Query(ast, input.KB.Bytecode);
        return q with {
            BuiltInHandlers = input.KB.BuiltInHandlers,
            AbstractTerms = input.KB.AbstractTerms,
            Source = input.KB
        };
    }
}
