using Ergo.Compiler.Emission;
using System.Diagnostics;

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
        var newB = E > B
            ? E + Code[Stack[E + 1] - 1] + 2
            : B + Stack[B] + 8;
        var n = Stack[newB] = N;
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(TryMeElse)}: L={l} B={B} E={E} CP={CP} H={H} TR={TR} B0={B0}");
#endif
        for (int i = 1; i <= n; i++)
            Stack[newB + i] = A[i - 1];
        Stack[newB + N + 1] = E;
        Stack[newB + N + 2] = CP;
        Stack[newB + N + 3] = B;
        Stack[newB + N + 4] = l;
        Stack[newB + N + 5] = TR;
        Stack[newB + N + 6] = H;
        Stack[newB + N + 7] = B0;
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
        var n = Stack[B];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(RetryMeElse)}: L={l} Rewinding to clause at P={P} TR={TR} H={H} CP={CP} B={B}");
#endif
        for (int i = 1; i <= n; i++)
            A[i - 1] = Stack[B + i];
        E = Stack[B + n + 1];
        CP = Stack[B + n + 2];
        Stack[B + n + 4] = l;
        unwind_trail(Stack[B + n + 5], TR);
        TR = Stack[B + n + 5];
        H = Stack[B + n + 6];
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
        var l = __addr();
        var n = Stack[B];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] {nameof(TrustMe)}: Cutting to last clause. Previous B={B} → B={Stack[B + n + 3]}");
#endif
        for (int i = 1; i <= n; i++)
            A[i - 1] = Stack[B + i];
        E = Stack[B + n + 1];
        CP = Stack[B + n + 2];
        Stack[B + n + 4] = l;
        unwind_trail(Stack[B + n + 5], TR);
        TR = Stack[B + n + 5];
        H = Stack[B + n + 6];
        B = Stack[B + n + 3];
        HB = Stack[B + n + 6];
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
            ? E + Code[Stack[E + 1] - 1] + 2
            : B + Stack[B] + 8;
        var n = Stack[newB] = N;
        for (int i = 1; i <= n; i++)
            Stack[newB + i] = A[i - 1];
        Stack[newB + N + 1] = E;
        Stack[newB + N + 2] = CP;
        Stack[newB + N + 3] = B;
        Stack[newB + N + 4] = P;
        Stack[newB + N + 5] = TR;
        Stack[newB + N + 6] = H;
        Stack[newB + N + 7] = B0;
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
        var n = Stack[B];
        for (int i = 1; i <= n; i++)
            A[i - 1] = Stack[B + i];
        E = Stack[B + n + 1];
        CP = Stack[B + n + 2];
        Stack[B + n + 4] = P;
        unwind_trail(Stack[B + n + 5], TR);
        TR = Stack[B + n + 5];
        H = Stack[B + n + 6];
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
        var n = Stack[B];
        for (int i = 1; i <= n; i++)
            A[i - 1] = Stack[B + i];
        E = Stack[B + n + 1];
        CP = Stack[B + n + 2];
        unwind_trail(Stack[B + n + 5], TR);
        TR = Stack[B + n + 5];
        H = Stack[B + n + 6];
        B = Stack[B + n + 3];
        HB = Stack[B + n + 6];
        P = l;
    }
    #endregion
}
