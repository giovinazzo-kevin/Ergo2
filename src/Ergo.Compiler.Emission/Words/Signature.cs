namespace Ergo.Compiler.Emission;

public readonly ref struct Signature
{
    public static readonly __WORD BITMASK = (__WORD)Math.Pow(2, BITCOUNT) - 1;
    public const int BITCOUNT = 6;

    public readonly __WORD RawValue;
    // Pointer to the constant table
    public readonly __WORD F;
    // Integer (arity; max = 2^ARITY_BITS - 1)
    public readonly __WORD N;

    private Signature(__WORD raw)
    {
        RawValue = raw;
        F = (raw & ~BITMASK) >> BITCOUNT;
        N = (raw & BITMASK);
    }

    private Signature(__WORD f, __WORD n)
    {
        RawValue = (f << BITCOUNT) | n;
        F = RawValue >> BITCOUNT;
        N = n;
    }


    public void Deconstruct(out __WORD f, out __WORD n)
    {
        f = F;
        n = N;
    }

    public static implicit operator Signature((__WORD f, __WORD n) x) => new(x.f, x.n);
    public static implicit operator Signature(__WORD rawValue) => new(rawValue);
    public static implicit operator __WORD(Signature word) => word.RawValue;
}
