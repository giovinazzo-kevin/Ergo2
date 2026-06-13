using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Pipelines.Compiler;
using Ergo.Pipelines.Parsing;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.Pipelines;

public static class KnowledgeBaseExtensions
{
    extension(KnowledgeBase kb)
    {
        public Pipeline<__string, Query> Query => Pipeline
            .WithStep(Parse.Instance, new Parse.Env { KB = kb })
            .WithStep(EmitQuery.Instance, new EmitQuery.Env { KB = kb });
    }
}
