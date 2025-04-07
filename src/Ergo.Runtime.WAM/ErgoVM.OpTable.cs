using Ergo.Compiler.Emission;
using Ergo.Shared.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region OpTable
    public static readonly __op[] OP_TABLE = new __op[(byte)Enum.GetValues<OpCode>().Length];
    static ErgoVM()
    {
        RuntimeHelpers.PrepareDelegate(__panic);
        for (byte i = 0; i < OP_TABLE.Length; ++i)
            OP_TABLE[i] = __static((OpCode)i);
    }
    protected static readonly __op __panic = __static(nameof(Panic));
    protected static __op __static(OpCode code) => __static(code.ToString().ToCSharpCase());
    protected static __op __static(string opName)
    {
        var op = typeof(ErgoVM)
            .GetMethod(opName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?
            .CreateDelegate<__op>();
        if (op is null)
            return __panic;
        RuntimeHelpers.PrepareDelegate(op);
        return op;
    }
    protected void Panic() => throw new NotImplementedException();
    #endregion
}
