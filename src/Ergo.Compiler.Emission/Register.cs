namespace Ergo.Compiler.Emission;

public readonly record struct Register(Register.Type Type_, int N)
{
    public enum Type
    {
        X,
        A,
        CP,
        E,
        B,
        H,
        TR
    }

    public static implicit operator Register((Type Type, int N) tuple) => new(tuple.Type, tuple.N);
}
