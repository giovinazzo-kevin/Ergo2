using Ergo.Compiler.Emission;
using Ergo.Lang.Ast;
using Ergo.Lang.Unification.Extensions;
using System.Text;

namespace Ergo.Runtime.WAM;

using static ErgoVM.State;
using static Op.Type;

public class ErgoVM
{
    public delegate void __op(ErgoVM vm, ref ReadOnlySpan<byte> span);

    public enum State
    {
        success,
        failure,
        halted
    }

    public State S { get; private set; } = success;
    public int PC { get; private set; } = 0;
    public readonly Term[] X = new Term[256];
    public readonly Term[] A = new Term[256];

    public static readonly __op[] OP_TABLE = new __op[(byte)OP_COUNT];

    static ErgoVM()
    {
#if DEBUG
        Array.Fill(OP_TABLE, op_unexpected);
#else
        Array.Fill(OP_TABLE, op_halt);
#endif
        OP_TABLE[(byte)halt] = op_halt;
        OP_TABLE[(byte)noop] = op_noop;
        OP_TABLE[(byte)get_variable] = op_get_variable;
        OP_TABLE[(byte)get_constant] = op_get_constant;
        OP_TABLE[(byte)put_value] = op_put_value;
        OP_TABLE[(byte)put_variable] = op_put_variable;
        OP_TABLE[(byte)put_constant] = op_put_constant;
        OP_TABLE[(byte)allocate] = op_allocate;
        OP_TABLE[(byte)deallocate] = op_deallocate;
        OP_TABLE[(byte)call] = op_call;
        OP_TABLE[(byte)proceed] = op_proceed;
        OP_TABLE[(byte)fail] = op_fail;
    }


    protected static byte __byte(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        var b = span[0];
        span = span[1..];
        return b;
    }
    protected static ReadOnlySpan<byte> __bytes(ErgoVM vm, ref ReadOnlySpan<byte> span, int num)
    {
        var b = span[..num];
        span = span[num..];
        return b;
    }
    protected static RuntimeType __runtime(ErgoVM vm, ref ReadOnlySpan<byte> span) 
        => new((RuntimeType.Type)__byte(vm, ref span));
    protected static int __int(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        var bytes = __bytes(vm, ref span, sizeof(int));
        var asInt = BitConverter.ToInt32(bytes);
        return asInt;
    }
    protected static string __string(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        var length = __int(vm, ref span);
        var bytes = __bytes(vm, ref span, length);
        var asString = Encoding.UTF8.GetString(bytes);
        return asString;
    }
    protected static Atom __const(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        Atom c = __runtime(vm, ref span).Type_ switch
        {
            RuntimeType.Type.__string => new __string(__string(vm, ref span)),
            RuntimeType.Type.__int => new __int(__int(vm, ref span)),
            _ => throw new NotSupportedException()
        };
        return c;
    }
    public static void op_unexpected(ErgoVM vm, ref ReadOnlySpan<byte> span) {
        ;
    }
    public static void op_noop(ErgoVM _, ref ReadOnlySpan<byte> __) { }
    public static void op_allocate(ErgoVM vm, ref ReadOnlySpan<byte> _)
    {
    }
    public static void op_deallocate(ErgoVM vm, ref ReadOnlySpan<byte> _)
    {
    }
    public static void op_fail(ErgoVM vm, ref ReadOnlySpan<byte> _)
    {
        vm.S = failure;
    }
    public static void op_proceed(ErgoVM vm, ref ReadOnlySpan<byte> _)
    {
    }
    public static void op_halt(ErgoVM vm, ref ReadOnlySpan<byte> _)
    {
        vm.S = halted;
    }
    public static void op_get_constant(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        var c = __const(vm, ref span);
        var Ai = __byte(vm, ref span);
        if (!c.Unify(vm.A[Ai]))
            op_fail(vm, ref span);
    }
    public static void op_get_variable(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
    }
    public static void op_put_value(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {

    }
    public static void op_put_variable(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        var Xn = __byte(vm, ref span);
        var Ai = __byte(vm, ref span);
        vm.A[Ai] = vm.X[Xn] = new Variable($"$X{Xn}", runtime: true);
    }
    public static void op_put_constant(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {
        var c = __const(vm, ref span);
        var Ai = __byte(vm, ref span);
        vm.A[Ai] = c;
    }
    public static void op_call(ErgoVM vm, ref ReadOnlySpan<byte> span)
    {

    }
<<<<<<< HEAD
    public unsafe void Query(Query query)
    {
    next_op:
        var span = query.Program.Span;
=======
    public unsafe void Run(KnowledgeBase kb, Query query)
    {
    next_op:
        var span = query.Memory.Span;
>>>>>>> e815e388bd85b6597a5fcb0cfa240c268b1249ee
        var op = __byte(this, ref span);
        OP_TABLE[op](this, ref span);
        switch (S)
        {
            case success: break;
            case failure:
                break;
            case halted: return;
        }
        goto next_op;
    }
}
