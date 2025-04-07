using Ergo.Compiler.Emission;
using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public event Action<ErgoVM> Solution = _ => { };

    public void Run(Query query)
    {
        _QUERY = query.Bytecode;
        P = _QUERY.QueryStart;
        while (true)
        {
            if (fail)
            {
                fail = false;
                backtrack();
            }

            var op = __word();
            OP_TABLE[op](this);

            if ((P == 0 || P >= Code.Length) && !fail)
            {
                EmitSolution();
                if (B > BOTTOM_OF_STACK)
                {
                    fail = true;
                    continue;
                }
                else break;
            }
        }

    }
}

