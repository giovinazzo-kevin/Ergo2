using Ergo.Lang.Ast;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Ergo.Compiler.Emission;

public abstract record Op(Op.Type Type_, int Size)
{
    public enum Type : byte
    {
        noop,

        get_variable,
        get_value,
        get_constant,
        get_structure,

        unify_variable,
        unify_value,
        unify_constant,
        unify_void,

        put_variable,
        put_value,
        put_constant,
        put_structure,
        
        set_variable,
        set_value,
        set_constant,
        set_void,

        call,
        execute,
        proceed,
        retry_me_else,
        trust_me_else_fail,
        try_me_else,

        allocate,
        deallocate,
        save_register,
        restore_register,

        switch_on_term,
        switch_on_constant,
        switch_on_structure,

        trail,
        undo_trail,

        cut,
        fail,
        halt
    }
    #region Declarations
    public static readonly Op noop = new NoOp();
    public static readonly Op proceed = new Proceed();
    public static readonly Func<string, byte, Op> call = (P, N) => new Call(P, N);
    public static readonly Func<Atom, byte, Op> get_constant = (c, Ai) => new GetConstant(c, Ai);
    public static readonly Func<byte, byte, Op> get_variable = (Xn, Ai) => new GetVariable(Xn, Ai);
    #endregion
    protected int EmitType(ref Span<byte> bytes)
    {
        return Emit(ref bytes, (byte)Type_);
    }
    protected int Emit(ref Span<byte> bytes, params ReadOnlySpan<byte> b)
    {
        b.CopyTo(bytes);
        bytes = bytes[b.Length..];
        return b.Length;
    }
    protected int EmitUTF8(ref Span<byte> bytes, string k)
    {
        if (k.Length > byte.MaxValue)
            throw new InvalidOperationException();
        EmitInt32(ref bytes, k.Length);
        var len = Encoding.UTF8.GetBytes(k, bytes);
        bytes = bytes[len..];
        return len + sizeof(int);
    }
    protected int EmitInt32(ref Span<byte> bytes, int k)
    {
        var intBytes = BitConverter.GetBytes(k);
        Emit(ref bytes, intBytes);
        return intBytes.Length;
    }
    protected int EmitConstant(ref Span<byte> bytes, Atom c) => c switch {
        __string => EmitUTF8(ref bytes, (string)c.Value),
        __int => EmitInt32(ref bytes, (int)c.Value),
        _ => 0
    };
    protected static int SizeOf(Atom c) => c switch
    {
        __string => sizeof(int) + Encoding.UTF8.GetByteCount((string)c.Value),
        __int => sizeof(int),
        _ => 0
    };
    public virtual int Emit(ref Span<byte> bytes) => 0;
    protected sealed record NoOp() : Op(Type.noop, 0);
    protected sealed record Proceed() : Op(Type.proceed, 1)
    {
        public override int Emit(ref Span<byte> bytes) => EmitType(ref bytes);
    }
    protected sealed record GetConstant(Atom c, byte Ai) : Op(Type.get_constant, SizeOf(c) + 2)
    {
        public override int Emit(ref Span<byte> bytes) =>
              EmitType(ref bytes)
            + EmitConstant(ref bytes, c)
            + Emit(ref bytes, Ai)
            ;
    }
    protected sealed record GetVariable(byte Xn, byte Ai) : Op(Type.get_variable, 3)
    {
        public override int Emit(ref Span<byte> bytes) =>
              EmitType(ref bytes)
            + Emit(ref bytes, Xn, Ai)
            ;
    }
    protected sealed record Call(string P, byte N) : Op(Type.call, Encoding.UTF8.GetByteCount(P) + 3)
    {
        public override int Emit(ref Span<byte> bytes) =>
              EmitType(ref bytes)
            + EmitUTF8(ref bytes, P)
            + Emit(ref bytes, N)
            ;
    }
}
