
using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public class Variable : Term
{
    public readonly string Name;

    protected Term _value;
    public Term Value
    {
        get => Deref();
        set => Bind(value);
    }
    public bool Bound => _value != this && _value is not Variable;
    public override bool Ground => _value != this && _value.Ground;
    public Variable(string name, bool runtime = false)
    {
        if (!runtime && !IsVariableIdentifier(name))
            throw new InvalidDataException();
        Name = name;
        _value = this;
    }
    protected Term Deref()
    {
        return _value switch
        {
            Variable { Bound: true } v => DerefVar(v),
            _ => _value
        };

        Term DerefVar(Variable v)
        {
            var visited = new HashSet<Variable>() { this };
            while (!visited.Contains(v))
            {
                visited.Add(v);
                if (v._value is not Variable { Bound: true } v1)
                    return v._value;
                v = v1;
            }
            return _value = v;
        }
    }

    protected void Bind(Term value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        _value = value;
    }

    public override string Expl => _value == this ? Name : _value.Expl.Parenthesized(IsParenthesized);
    public override bool Equals(object? obj) => _value == this ? obj == this : _value.Equals(obj);
    public override int GetHashCode() => Name.GetHashCode();

    public static implicit operator Variable(string name) => new(name);
    public static bool IsVariableIdentifier(string s) => s[0] == '_' || char.IsLetter(s[0]) && char.IsUpper(s[0]);
}
