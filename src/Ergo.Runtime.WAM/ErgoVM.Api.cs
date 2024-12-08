using Ergo.Compiler.Emission;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public event Action<ErgoVM> Solution = _ => { };

    public void Run(Query query)
    {
        _QUERY = query.Bytecode;
        P = _QUERY.QueryStart;
        while (P < Code.Length)
        {
            var op = __word();
            OP_TABLE[op](this);
            if (fail)
                backtrack();
            else if ((OpCode)op == OpCode.proceed)
                Solution.Invoke(this);
        }
    }
}
