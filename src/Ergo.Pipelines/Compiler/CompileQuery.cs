using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;
using ParseDelegate = Ergo.Lang.Parsing.WellKnown.Delegates.Parse;

namespace Ergo.Pipelines.Compiler;

public class CompileQuery : IPipeline<string, Query, CompileQuery.Env>
{
    public class Env
    {
        public required KnowledgeBase KB { get; init; }
    }

    internal static readonly CompileQuery Instance = new();
    private CompileQuery() { }

    public Result<Query, PipelineError> Run(string input, Env env)
    {
        var kb = env.KB;
        var file = ErgoFileStream.Create(input, nameof(CompileQuery));
        using var lexer = new Lexer(file, kb.Bytecode.Operators);
        using var parser = new Parser(lexer);
        foreach (var abs in kb.AbstractTerms.Values) {
            if (abs.Parse == null) continue;
            var factory = (ParseDelegate)abs.Parse;
            var production = factory(parser);
            if (production != null)
                parser.AddAbstractParser(production);
        }
        if (!parser.BinaryExpressionRhs().TryGetValue(out var ast))
            return new PipelineError(this, new InvalidOperationException("Failed to parse query"));
        var emitter = new Emitter();
        var q = emitter.Query(ast, kb.Bytecode);
        return q with {
            BuiltInHandlers = kb.BuiltInHandlers,
            AbstractTerms = kb.AbstractTerms,
            Reconstructors = kb.Reconstructors,
            Source = kb
        };
    }
}
