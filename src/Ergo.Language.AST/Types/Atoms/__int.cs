﻿using Ergo.Shared.Extensions;

namespace Ergo.Lang.Ast;

public sealed class __int(int value) : Atom(typeof(__int), value)
{
    public static implicit operator __int(int n) => new(n);
    public static implicit operator int(__int n) => (int)n.Value;
    public static bool operator ==(__int a, __int b) => (int)a.Value == (int)b.Value;
    public static bool operator !=(__int a, __int b) => (int)a.Value != (int)b.Value;
    public static bool operator ==(__int a, int b) => (int)a.Value == b;
    public static bool operator !=(__int a, int b) => (int)a.Value != b;
    public override string Expl => $"{Value}"!.Parenthesized(IsParenthesized);
    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
    public override Term Clone() => (__int)value;
}
