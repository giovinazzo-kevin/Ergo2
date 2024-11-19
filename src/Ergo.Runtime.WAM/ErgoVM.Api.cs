using Ergo.Compiler.Emission;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    public void Init(Query query)
    {
        _ROM = query.Code;
        P = query.Start;
        _labels.Clear();
    }

    public void Run()
    {
        while(P < Code.Length)
            OP_TABLE[__word()](this);
    }
}
