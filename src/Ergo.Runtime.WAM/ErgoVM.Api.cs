using Ergo.Compiler.Emission;
using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public event Action<ErgoVM> SolutionEmitted = _ => { };
    private int _traceLevel = 0;

    public void Run(Query query)
    {
        CP = int.MaxValue;
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        P = _QUERY.QueryStart;
        exit = fail = false;
        while (!exit)
        {
            if (fail)
            {
                if (backtrack())
                    break;
                continue; // Go to next choice point
            }

            // Execution halts naturally when we run out of code
            if (P >= Code.Length)
            {
                break;
            }

            var op = __word();
            OP_TABLE[op](this);
        }
    }

}

