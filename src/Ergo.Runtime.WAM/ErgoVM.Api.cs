using Ergo.Compiler.Emission;
using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public event Action<ErgoVM> SolutionEmitted = _ => { };

    public void Run(Query query)
    {
        CP = int.MaxValue;
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        _NAMES = _VARS.ToDictionary(x => x.Value.Index, x => x.Value);
        P = _QUERY.QueryStart;

        while (true)
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

