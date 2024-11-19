namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Constants
    protected const int HEAP_SIZE = 1024;
    protected const int STACK_SIZE = 1024;
    protected const int DEFAULT_TRAIL_SIZE = 1024;
    protected const int MAX_ARGS = 256;
    protected const int MAX_TMPS = 256;

    protected const int BOTTOM_OF_STACK = 0;
    #endregion
}
