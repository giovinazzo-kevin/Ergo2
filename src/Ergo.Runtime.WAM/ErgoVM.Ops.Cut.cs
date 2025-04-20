using System.Diagnostics;

namespace Ergo.Runtime.WAM;
public partial class ErgoVM
{
    #region Cut Instructions
    /// <summary>
    /// If there is a choice point after that indicated
    /// by B0, discard it and tidy the trail up to that
    /// point.
    /// Continue execution with the following instruction.
    /// </summary>
    protected void NeckCut()
    {
#if WAM_TRACE
        Trace.WriteLine("[WAM] NeckCut");
#endif
        if (B <= B0) return;
        B = B0;
        tidy_trail();
    }
    /// <summary>
    /// Set Yn to the current value of B0.
    /// Continue execution with the following instruction.
    /// </summary>
    protected void GetLevel()
    {
#if WAM_TRACE
        Trace.WriteLine("[WAM] GetLevel");
#endif
        var n = __word();
        Stack[E + 2 + n] = B0;
    }
    /// <summary>
    /// Discard all (if any) choice points after that
    /// indicated by Yn, and tidy the trail up to that point.
    /// Continue execution with the following instruction.
    /// </summary>
    protected void Cut()
    {
        var n = __word();
        var Yn = Stack[E + 2 + n];
#if WAM_TRACE
        Trace.WriteLine($"[WAM] CUT! n={n} Yn={Yn} B={B}");
#endif

        // Actively remove choice points after the cut
        while (B > Yn)
        {
            // Restore trail and heap if needed
            unwind_trail(B, TR);
            B = Stack[B + Stack[B] + 3]; // previous choice point (saved B)
        }

        tidy_trail(); // Clean up trail after cut
    }
    #endregion
}
