using Ergo.Compiler.Emission;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Type Readers
    protected __WORD __word() => Code[P++];
    protected object __const() => Constants[Code[P++]];
    protected __ADDR __addr() => __word();
    protected Signature __signature() => Code[P++];
    protected Term __term() => Code[P++];
    #endregion
}
