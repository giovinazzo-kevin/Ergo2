namespace Ergo.Lang.Ast;

public sealed class __bool(bool value) : Atom(typeof(__bool), value) 
{ 
    public static implicit operator __bool(bool n) => new(n);
    public override string Expl => Value is true ? "⊤" : "⊥";
}
