using Ergo.Lang.Ast;
using System.Text;

namespace Ergo.Compiler.Emission;

public record Op(OpCode Code, params Func<__WORD>[] Args)
{
    public readonly int Size = Args.Length + 1;

    public virtual int Emit(ref Span<__WORD> words)
    {
        int size = EmitOpType(ref words);
        for (int i = 0; i < Args.Length; i++)
            size += Emit(ref words, Args[i]());
        return size;
    }
    protected int EmitOpType(ref Span<__WORD> words)
    {
        return Emit(ref words, (__WORD)Code);
    }
    protected int Emit(ref Span<__WORD> words, params ReadOnlySpan<__WORD> b)
    {
        b.CopyTo(words);
        words = words[b.Length..];
        return b.Length;
    }
}