using Ergo.Compiler.Emission;
using Microsoft.VisualBasic;
using static Ergo.Compiler.Analysis.CallGraph;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    protected virtual void Initialize()
    {

    }

    public void Run(KnowledgeBase kb, Query query)
    {
        Initialize();
        (P, _BYTECODE) = (0, query.Bytecode);
        while (P < Code.Length)
            OP_TABLE[__word()](this);
        (P, _BYTECODE) = (0, kb.Bytecode);
    }
}
