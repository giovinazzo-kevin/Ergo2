using Ergo.Compiler.Emission;
using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public event Action<ErgoVM> SolutionEmitted = _ => { };

    public void Run(Query query)
    {
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        P = _QUERY.QueryStart;
        while (true)
        {
            if (fail && backtrack())
                break;
            else if (P == 0 || P >= Code.Length)
            {
                EmitSolution();
                if (B > BOTTOM_OF_STACK)
                {
                    fail = true;
                    continue;
                }
                break;
            }
            var op = __word();
            OP_TABLE[op](this);
        }

    }
}

