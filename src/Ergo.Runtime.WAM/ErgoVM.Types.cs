
global using static Ergo.Compiler.Emission.Term.__TAG;
global using __ADDR = int;
global using __WORD = int;
using Ergo.Lang.Ast;

namespace Ergo.Runtime.WAM;

public partial class ErgoVM
{
    // Abstract term delegate types — defined here because ErgoVM is the parameter type
    public delegate void __abs_unify(ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo);
    public delegate Lang.Ast.Term __abs_read(ErgoVM vm, int addr);
    public delegate int __abs_write_heap(ErgoVM vm, Lang.Ast.Term term);
    public delegate string __abs_pretty(ErgoVM vm, int addr, bool quoted);

    #region Types
    public delegate void __op(ErgoVM vm);
    public enum GetMode
    {
        read,
        write
    }
    public readonly record struct Binding(string Variable, Term Value)
    {
        public override string ToString() => $"{Variable}/{Value.Expl}";
    }
    public readonly struct Solution(Binding[] bindings)
    {
        public readonly Binding[] Bindings = bindings;
        public override string ToString() => String.Join(", ", Bindings);
    }
    #endregion
}
