using Ergo.Compiler.Emission;
using Microsoft.VisualBasic;
using static Ergo.Compiler.Analysis.CallGraph;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public void Run(Query query)
    {
        _QUERY = query.Bytecode;
        P = _QUERY.QueryStart;
        while (P < _QUERY.Code.Length)
            OP_TABLE[Code[P]](this);
    }
}
