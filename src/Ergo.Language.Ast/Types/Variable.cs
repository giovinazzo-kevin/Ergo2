
using Ergo.Shared.Extensions;

namespace Ergo.Language.Ast;

public class Variable : Term
{
    public readonly string Name;
    public Term Value { get; set; }
    public bool Bound => Value != this && Value is not Variable;
    public override bool Ground => Bound && Value.Ground;
    public Variable(string name)
    {
        if (!IsVariableIdentifier(name))
            throw new InvalidDataException();
        Name = name;
        Value = this;
    }
    public override string Expl => (!Bound ? Name : Value is Variable v ? v.Name : Value.Expl).Parenthesized(IsParenthesized);
    public override bool Equals(object? obj) => !Bound ? obj == this : Value.Equals(obj);
    public override int GetHashCode() => !Bound ? Name.GetHashCode() : Value.GetHashCode();

    public static implicit operator Variable(string name) => new(name);
    public static bool IsVariableIdentifier(string s) => s[0] == '_' || char.IsLetter(s[0]) && char.IsUpper(s[0]);
}
