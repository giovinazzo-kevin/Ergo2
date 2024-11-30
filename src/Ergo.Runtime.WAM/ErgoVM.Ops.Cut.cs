﻿namespace Ergo.Runtime.WAM;
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
        if (B <= Yn)
            return;
        B = Yn;
        tidy_trail();
    }
    #endregion
}