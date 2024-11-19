global using __WORD = int;
namespace Ergo.Compiler.Emission;

public static class WordExtensions
{
    public static __WORD NextPowerOfTwo(this __WORD v)
    {
        v--;
        for (var i = 1; i < sizeof(__WORD); i *= 2)
            v |= v >> i;
        return ++v;
    }
    public static byte CountBits(this __WORD value)
    {
        byte count = 0;
        while (value != 0)
        {
            count++;
            value &= value - 1;
        }
        return count;
    }
}