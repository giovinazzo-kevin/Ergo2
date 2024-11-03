namespace Ergo.Language.Ast;

public sealed class __string(string Value) : Atom(typeof(__string), Value) { public static implicit operator __string(string s) => new(s); };
