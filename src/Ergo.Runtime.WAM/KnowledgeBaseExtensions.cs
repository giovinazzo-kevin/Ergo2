using Ergo.Compiler.Emission;
using Ergo.Shared.Types;

namespace Ergo.Runtime.WAM;

public static class KnowledgeBaseExtensions
{
    public static Maybe<Hook> Hook(this KnowledgeBase kb, Lang.Ast.Signature sig)
    {
        if (!kb.Bytecode.TryResolve(sig, out var resolved))
            return Maybe<Hook>.None;
        return new Hook(kb, resolved);
    }
}
