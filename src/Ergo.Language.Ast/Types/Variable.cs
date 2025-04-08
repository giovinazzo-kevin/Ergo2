
using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;
public class Variable : Term
{
    public readonly string Name;

    protected Term _value;
    public Term Value
    {
        get => _value ?? this;
        set => _value = value;
    }

    public bool IsBound => _value != null;
    public override bool IsGround => IsBound && _value.IsGround;

    public Variable(string name, bool runtime = false)
    {
        if (!runtime && !IsVariableIdentifier(name))
            throw new InvalidDataException();
        Name = name;
        _value = null!;
    }

    public override string Expl
        => (_value?.Expl ?? Name)
        .Parenthesized(IsParenthesized);
    public override bool Equals(object? obj) => !IsBound ? obj == this : (obj?.Equals(_value) ?? false);
    public override int GetHashCode() => Name.GetHashCode();

    public static implicit operator Variable(string name) => new(name);
    public static bool IsVariableIdentifier(string s) => s[0] == '_' || char.IsLetter(s[0]) && char.IsUpper(s[0]);
    public override Term Clone() => new Variable(Name, runtime: true) { Value = _value?.Clone()! };
}
