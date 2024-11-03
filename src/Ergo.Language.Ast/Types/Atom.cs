using Ergo.Shared.Extensions;

namespace Ergo.Language.Ast;

public abstract class Atom : Term
{
    public readonly Type Type;
    public readonly object Value;
    public override bool Ground => true;
    public Atom(Type type, object value)
    {
        Type = type;
        Value = value;
    }
    public override string Expl => Value.ToString()!.Parenthesized(IsParenthesized).AddQuotesIfNecessary();
    public override bool Equals(object? obj) => obj is Atom { Type: var type, Value: var value } && Equals(type, Type) && Equals(value, Value);
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator Atom(string s) => new __string(s);
    public static implicit operator Atom(bool b) => new __bool(b);
    public static implicit operator Atom(double d) => new __double(d);
    public static bool IsAtomIdentifier(string s) => !Variable.IsVariableIdentifier(s);
}
