using Ergo.Compiler.Emission;
using Ergo.Shared.Types;
using AstTerm = Ergo.Lang.Ast.Term;

namespace Ergo.Pipelines.Compiler;

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
        foreach (var abs in kb.AbstractTerms.Values)
            emitter.RegisterAbstractTermEmitter(abs, EmitterContext.From(kb.Bytecode));
        var q = emitter.Query(ast, kb.Bytecode);
        return q with { Source = kb };
    }
}
