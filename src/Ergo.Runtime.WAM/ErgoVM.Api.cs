using Ergo.Compiler.Emission;
using Query = Ergo.Compiler.Emission.Query;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    public event Action<ErgoVM> SolutionEmitted = _ => { };
    private int _traceLevel = 0;

    public void Run(Query query)
    {
        KB = query.Source;
        CP = int.MaxValue;
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        if (_dynamics.Count > 0)
            RehydrateDynamicCode();
        _dynConts.Clear();
        _inDynClause = false;
        P = _QUERY.QueryStart;
        E = HEAP_SIZE;
        B = HEAP_SIZE;
        H = 0;
        TR = 0;
        exit = fail = false;
        while (!exit) {
            if (fail) {
                if (backtrack())
                    break;
                continue;
            }
            if (P >= Code.Length) {
                EmitSolution();
                break;
            }
            var op = __word();
            OP_TABLE[op](this);
        }
    }

}
