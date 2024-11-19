using Ergo.Compiler.Analysis;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ergo.Compiler.Emission;

public sealed partial class KnowledgeBase
{
    private static readonly int HASH_BOOL = typeof(bool).GetHashCode();
    private static readonly int HASH_INT32 = typeof(int).GetHashCode();
    private static readonly int HASH_DOUBLE = typeof(double).GetHashCode();
    private static readonly int HASH_STRING = typeof(string).GetHashCode();

    public readonly string Name;
    private readonly __WORD[] Data;
    private readonly List<Atom> Constants;
    private readonly OperatorLookup Operators;

    private readonly int S_Code;
    public ReadOnlySpan<__WORD> Code => Data.AsSpan(S_Code);

    public KnowledgeBase(ErgoFileStream file)
        : this(file.Name, ReadFile(file)) { }

    public KnowledgeBase(string name, __WORD[] data)
    {
        Name = name;
        Data = data;
        Operators = new();
        Constants = [];
        var span = Data.AsSpan();
        var numOfConstants = span[0]; span = span[1..];
        for (int i = 0; i < numOfConstants; i++)
            Constants.Add(DeserializeConstant(ref span));
        var numOfOperators = span[0]; span = span[1..];
        var ops = new List<Operator>();
        for (int i = 0; i < numOfOperators; i++)
            ops.Add(DeserializeOperator(ref span));
        Operators.AddRange(ops);
        S_Code = Data.Length - span.Length;
    }

    public void Query(string query)
    {

    }

    private static __WORD[] ReadFile(ErgoFileStream file)
    {
        return [];
    }

    private Operator DeserializeOperator(ref Span<__WORD> span)
    {
        var numOfFunctors = span[0];
        var precedence = span[1];
        var type = (Operator.Type)span[2];
        span = span[3..];
        var functors = new Atom[numOfFunctors];
        for (int i = 0; i < numOfFunctors; i++)
            functors[i] = Constants[span[i]];
        span = span[numOfFunctors..];
        return new Operator(precedence, type, functors);
    }

    private Atom DeserializeConstant(ref Span<__WORD> span)
    {
        var type = span[0];
        if (type == HASH_BOOL)
        {
            var asBool = span[1] != 0;
            span = span[2..];
            return asBool;
        }
        if (type == HASH_INT32)
        {
            var asInt = span[1];
            span = span[2..];
            return asInt;
        }
        if (type == HASH_DOUBLE)
        {
            var asDouble = BitConverter.UInt64BitsToDouble((ulong)(span[1] << 32) | (ulong)span[2]);
            span = span[3..];
            return asDouble;
        }
        if (type == HASH_STRING)
        {
            var lenInWords = span[1];
            span = span[2..];
            var words = span[..lenInWords];
            var bytes = new byte[lenInWords * sizeof(__WORD)];
            for (int j = 0; j < lenInWords; ++j)
            {
                bytes[j * 4 + 0] = (byte)(words[j] >> 0);
                bytes[j * 4 + 1] = (byte)(words[j] >> 8);
                bytes[j * 4 + 2] = (byte)(words[j] >> 16);
                bytes[j * 4 + 3] = (byte)(words[j] >> 24);
            }
            span = span[lenInWords..];
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
        throw new NotSupportedException();
    }
}
