using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public abstract class Atom : Term
{
    public readonly Type Type;
    public readonly object Value;
    public override bool IsGround => true;
    public Atom(Type type, object value)
    {
        Type = type;
        Value = value;
    }
    public override string Expl => Value.ToString()!.Parenthesized(IsParenthesized).AddQuotesIfNecessary();
    public override bool Equals(object? obj) 
        => obj is Atom { Type: var type, Value: var value } && Equals(type, Type) && Equals(value, Value)
        || obj is Variable { Value: Atom other } && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator Atom(string s) => new __string(s);
    public static implicit operator Atom(bool b) => new __bool(b);
    public static implicit operator Atom(double d) => new __double(d);
    public static bool operator ==(Atom a, __string b) => a.Value == b.Value;
    public static bool operator !=(Atom a, __string b) => a.Value != b.Value;
    public static bool operator ==(Atom a, string b) => (string)a.Value == b;
    public static bool operator !=(Atom a, string b) => (string)a.Value != b;
    public static bool operator ==(Atom a, __int b) => a.Value == b.Value;
    public static bool operator !=(Atom a, __int b) => a.Value != b.Value;
    public static bool operator ==(Atom a, int b) => (int)a.Value == b;
    public static bool operator !=(Atom a, int b) => (int)a.Value != b;
    public static bool IsAtomIdentifier(string s) => !Variable.IsVariableIdentifier(s);
}
