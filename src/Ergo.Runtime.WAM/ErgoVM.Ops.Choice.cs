namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    #region Choice Instructions
    /// <summary>
    /// Allocate a new choice point frame on the stack setting
    /// its next clause field to L and the other fields according
    /// to the current context, and set B to point to it.
    /// Continue execution with the following instruction.
    /// </summary>
    public void TryMeElse()
    {
        var l = __addr();
        var es = envsize();
        var newB = E > B
            ? E + es + 2
            : B + Store[B] + 8;
        var n = Store[newB] = N;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(TryMeElse)} (1): newB={newB} L={l} B={B} B0={B0} HB={HB} E={E} CP={CP} H={H} TR={TR}");
#endif
        for (int i = 1; i <= n; i++)
            Store[newB + i] = A[i - 1];
        Store[newB + N + 1] = E;
        Store[newB + N + 2] = CP;
        Store[newB + N + 3] = B;
        Store[newB + N + 4] = l;
        Store[newB + N + 5] = TR;
        Store[newB + N + 6] = H;
        Store[newB + N + 7] = B0;
        B = newB;
        HB = H;
    }
    /// <summary>
    /// Having backtracked to the current choice point,
    /// reset all the necessary information from it and
    /// update its next clause field to L.
    /// Continue execution with the following instruction.
    /// </summary>
    public void RetryMeElse()
    {
        var l = __addr();
        var n = Store[B];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(RetryMeElse)}: L={l} Rewinding to clause at P={P} TR={TR} H={H} CP={CP} B={B}");
#endif
        for (int i = 1; i <= n; i++)
            A[i - 1] = Store[B + i];
        E = Store[B + n + 1];
        CP = Store[B + n + 2];
        Store[B + n + 4] = l;
        unwind_trail(Store[B + n + 5], TR);
        TR = Store[B + n + 5];
        H = Store[B + n + 6];
        HB = H;
    }
    /// <summary>
    /// Having backtracked to the current choice point,
    /// reset all the necessary information from it,
    /// then discard it by resetting B to its predecessor.
    /// Continue execution with the following instruction.
    /// </summary>
    public void TrustMe()
    {
        var n = Store[B];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(TrustMe)}: Cutting to last clause. Previous B={B} → B={Store[B + n + 3]}");
#endif
        for (int i = 1; i <= n; i++)
            A[i - 1] = Store[B + i];
        E = Store[B + n + 1];
        CP = Store[B + n + 2];
        unwind_trail(Store[B + n + 5], TR);
        TR = Store[B + n + 5];
        H = Store[B + n + 6];
        B = Store[B + n + 3];
        HB = (B == BOTTOM_OF_STACK) ? 0 : Store[B + Store[B] + 6];
    }
    /// <summary>
    /// Allocate a new choice point frame on the stack
    /// setting its next clause field to the following
    /// instruction and the other fields according to
    /// the current context, and set B to point to it.
    /// Continue execution with the following instruction.
    /// </summary>
    public void Try()
    {
#if WAM_TRACE
        Trace.WriteLine(nameof(Try));
#endif
        var l = __addr();
        var newB = E > B
            ? E + envsize() + 2
            : B + Store[B] + 8;
        var n = Store[newB] = N;
        for (int i = 1; i <= n; i++)
            Store[newB + i] = A[i - 1];
        Store[newB + N + 1] = E;
        Store[newB + N + 2] = CP;
        Store[newB + N + 3] = B;
        Store[newB + N + 4] = P;
        Store[newB + N + 5] = TR;
        Store[newB + N + 6] = H;
        Store[newB + N + 7] = B0;
        B = newB;
        HB = H;
        P = l;
    }
    /// <summary>
    /// Having backtracked to the current choice point,
    /// reset all the necessary information from it and
    /// update its next clause field to the following 
    /// instruction.
    /// Continue execution with instruction labeled L.
    /// </summary>
    public void Retry()
    {
#if WAM_TRACE
        Trace.WriteLine(nameof(Retry));
#endif
        var l = __addr();
        var n = Store[B];
        for (int i = 1; i <= n; i++)
            A[i - 1] = Store[B + i];
        E = Store[B + n + 1];
        CP = Store[B + n + 2];
        Store[B + n + 4] = P;
        unwind_trail(Store[B + n + 5], TR);
        TR = Store[B + n + 5];
        H = Store[B + n + 6];
        HB = H;
        P = l;
    }
    /// <summary>
    /// Having backtracked to the current choice point,
    /// reset all the necessary information from it,
    /// then discard it by resetting B to its predecessor.
    /// Continue execution with instruction labeled L.
    /// </summary>
    public void Trust()
    {
#if WAM_TRACE
        Trace.WriteLine(nameof(Trust));
#endif
        var l = __addr();
        var n = Store[B];
        for (int i = 1; i <= n; i++)
            A[i - 1] = Store[B + i];
        E = Store[B + n + 1];
        CP = Store[B + n + 2];
        unwind_trail(Store[B + n + 5], TR);
        TR = Store[B + n + 5];
        H = Store[B + n + 6];
        B = Store[B + n + 3];
        HB = (B == BOTTOM_OF_STACK) ? 0 : Store[B + Store[B] + 6];
        P = l;
    }
    #endregion
}
