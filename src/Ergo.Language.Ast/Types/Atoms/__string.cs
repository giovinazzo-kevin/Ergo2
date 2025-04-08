namespace Ergo.Lang.Ast;

public sealed class __string(string value) : Atom(typeof(__string), value) 
{ 
    public static implicit operator __string(string s) => new(s); 
    public static implicit operator string(__string s) => (string)s.Value;
    public static bool operator ==(__string a, __string b) => (string)a.Value == (string)b.Value;
    public static bool operator !=(__string a, __string b) => (string)a.Value != (string)b.Value;
    public static bool operator ==(__string a, string b) => (string)a.Value == b;
    public static bool operator !=(__string a, string b) => (string)a.Value != b;
    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override Term Clone() => (__string)value;
}
