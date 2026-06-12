using Ergo.Lang.Ast;

namespace Ergo.Runtime.WAM.WellKnown;

public static class Delegates
{
    public delegate void Unify(ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo);
    public delegate Term Read(ErgoVM vm, int addr);
    public delegate int WriteHeap(ErgoVM vm, Term term);
    public delegate string Pretty(ErgoVM vm, int addr, bool quoted);
}
