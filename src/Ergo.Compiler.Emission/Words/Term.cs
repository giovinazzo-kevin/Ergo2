
using System;

namespace Ergo.Compiler.Emission;

public readonly ref struct Term
{
    public static readonly __WORD BITMASK = Enum.GetValues<__TAG>().Length.NextPowerOfTwo() - 1;
    public static readonly byte BITCOUNT = BITMASK.CountBits();

    public readonly __WORD RawValue;
    public readonly __WORD Value;
    public readonly __TAG Tag;

    public enum __TAG : byte
    {
        CON,
        STR,
        LIS,
        REF
    }

    private Term(__WORD raw)
    {
        RawValue = raw;
        Tag = (__TAG)(raw & BITMASK);
        Value = raw >> BITCOUNT;
    }

    private Term(__TAG tag, __WORD value)
    {
        RawValue = (value << BITCOUNT) | (__WORD)tag;
        Value = RawValue >> BITCOUNT;
        Tag = tag;
    }

    public void Deconstruct(out __TAG tag, out __WORD value)
    {
        tag = Tag;
        value = Value;
    }

    public static implicit operator Term((__TAG Tag, __WORD Value) x) => new(x.Tag, x.Value);
    public static implicit operator Term(__WORD rawValue) => new(rawValue);
    public static implicit operator __WORD(Term word) => word.RawValue;
}
