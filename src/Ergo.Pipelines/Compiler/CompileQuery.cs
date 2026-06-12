using Ergo.Compiler.Emission;
using Ergo.IO;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;
using AstTerm = Ergo.Lang.Ast.Term;
using ParseDelegate = Ergo.Lang.Parsing.WellKnown.Delegates.Parse;

namespace Ergo.Pipelines.Compiler;

public class CompileQuery : IPipeline<KnowledgeBase, Pipeline<string, Query>, CompileQuery.Env>
{
    public class Env { }

    internal static readonly CompileQuery Instance = new();
    private CompileQuery() { }

    public Result<Pipeline<string, Query>, PipelineError> Run(KnowledgeBase kb, Env env)
    {
        return Pipeline.WithStep(CreateParser.Instance, new CreateParser.Env { KB = kb })
            .WithStep(RegisterAbstractParsers.Instance, new RegisterAbstractParsers.Env { KB = kb })
            .WithStep(Parse.Instance)
            .WithStep(EmitQuery.Instance, new EmitQuery.Env { KB = kb });
    }
}

public class CreateParser : IPipeline<string, Parser, CreateParser.Env>
{
    public class Env
    {
        public KnowledgeBase KB { get; init; } = null!;
    }

    internal static readonly CreateParser Instance = new();
    private CreateParser() { }

    public Result<Parser, PipelineError> Run(string input, Env env)
    {
        var file = ErgoFileStream.Create(input, nameof(CompileQuery));
        var lexer = new Lexer(file, env.KB.Bytecode.Operators);
        return new Parser(lexer);
    }
}

public class RegisterAbstractParsers : IPipeline<Parser, Parser, RegisterAbstractParsers.Env>
{
    public class Env
    {
        public KnowledgeBase KB { get; init; } = null!;
    }

    internal static readonly RegisterAbstractParsers Instance = new();
    private RegisterAbstractParsers() { }

    public Result<Parser, PipelineError> Run(Parser parser, Env env)
    {
        foreach (var abs in env.KB.AbstractTerms.Values) {
            var factory = (ParseDelegate)abs.Parse;
            parser.AddAbstractParser(factory(parser));
        }
        return parser;
    }
}

public class Parse : IPipeline<Parser, AstTerm, Parse.Env>
{
    public class Env { }

    internal static readonly Parse Instance = new();
    private Parse() { }

    public Result<AstTerm, PipelineError> Run(Parser parser, Env env)
    {
        if (!parser.BinaryExpressionRhs().TryGetValue(out var ast))
            return new PipelineError(this, new InvalidOperationException("Failed to parse query"));
        return ast;
    }
}

public class EmitQuery : IPipeline<AstTerm, Query, EmitQuery.Env>
{
    public class Env
    {
        public KnowledgeBase KB { get; init; } = null!;
    }

    internal static readonly EmitQuery Instance = new();
    private EmitQuery() { }

    public Result<Query, PipelineError> Run(AstTerm ast, Env env)
    {
        var kb = env.KB;
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
