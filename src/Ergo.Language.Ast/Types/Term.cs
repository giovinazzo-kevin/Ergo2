using Ergo.Shared.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{Expl}")]
public abstract class Term : IExplainable
{ 
    public bool IsParenthesized { get; set; }
    public abstract bool IsGround { get; }
    public abstract string Expl { get; }
    public override string ToString() => Expl;
    public static implicit operator Term(string s) => Variable.IsVariableIdentifier(s) ? new Variable(s) : new __string(s);
    public static implicit operator Term(bool b) => new __bool(b);
    public static implicit operator Term(double d) => new __double(d);
}

