using Ergo.Lang.Ast;

namespace Ergo.Compiler.Emission;

public readonly record struct RuntimeType(RuntimeType.Type Type_)
{
    public enum Type : byte
    {
        __string,
        __int,
        __double,
        __bool,
        Variable,
        Complex
    }

    public static RuntimeType FromTerm(Term t) => t switch
    {
        __string => new(Type.__string),
        __int => new(Type.__int),
        __double => new(Type.__double),
        __bool => new(Type.__bool),
        Variable => new(Type.Variable),
        Complex => new(Type.Complex),
        _ => throw new NotSupportedException()
    };
}
