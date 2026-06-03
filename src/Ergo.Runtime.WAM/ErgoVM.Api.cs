using Ergo.Compiler.Emission;
using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public readonly List<__op> BuiltIns = [];

    public void RegisterBuiltIn(KnowledgeBase kb, string name, int arity, __op handler)
    {
        var idx = kb.RegisterBuiltInLabel(name, arity);
        while (BuiltIns.Count <= idx) BuiltIns.Add(null!);
        BuiltIns[idx] = handler;
    }

    public event Action<ErgoVM> SolutionEmitted = _ => { };
    private int _traceLevel = 0;

    public void Run(Query query)
    {
        CP = int.MaxValue;
        _QUERY = query.Bytecode;
        _VARS = query.Variables;
        P = _QUERY.QueryStart;
        E = HEAP_SIZE;
        B = HEAP_SIZE;
        H = 0;
        TR = 0;
        exit = fail = false;
        while (!exit)
        {
            if (fail)
            {
                if (backtrack())
                    break;
                continue; // Go to next choice point
            }

            // Solution found: query code ran to completion
            if (P >= Code.Length)
            {
                EmitSolution();
                fail = true;
                continue; // Try to backtrack for more solutions
            }

            var op = __word();
            OP_TABLE[op](this);
        }
    }

}

