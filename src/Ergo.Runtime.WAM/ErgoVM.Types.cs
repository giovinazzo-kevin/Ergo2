
global using static Ergo.Compiler.Emission.Term.__TAG;
global using __WORD = int;
global using __ADDR = int;
using Ergo.Lang.Ast;

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
    public readonly record struct Binding(string Variable, Term Value)
    {
        public override string ToString() => $"{Variable}/{Value.Expl}";
    }
    public readonly ref struct Solution(ReadOnlySpan<Binding> bindings)
    {
        public readonly ReadOnlySpan<Binding> Bindings = bindings;
        public override string ToString() => String.Join(", ", Bindings.ToArray());
    }
    #endregion
}
