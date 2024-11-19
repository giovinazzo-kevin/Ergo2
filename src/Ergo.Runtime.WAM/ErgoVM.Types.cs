
global using static Ergo.Compiler.Emission.Term.__TAG;
global using __WORD = int;
global using __ADDR = int;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Types
    public delegate void __op(ErgoVM vm);
    public enum GetMode
    {
        read,
        write
    }
    #endregion
}
