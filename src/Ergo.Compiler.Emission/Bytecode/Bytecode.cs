using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Shared.Extensions;
using System;
using System.Text;
using static Ergo.Compiler.Emission.Term;
using static Ergo.Compiler.Emission.Term.__CONST_TAG;

namespace Ergo.Compiler.Emission;

public abstract class Bytecode
{
    public delegate Atom ConstantDeserializer(ref ReadOnlySpan<__WORD> span);
    public delegate void ConstantSerializer(Span<__WORD> buffer, Atom value, out int wordsWritten);

    private static readonly Dictionary<__CONST_TAG, ConstantDeserializer> _constantDeserializers = new();
    private static readonly Dictionary<__CONST_TAG, ConstantSerializer> _constantSerializers = new();

    public static IEnumerable<__CONST_TAG> RegisteredTags => _constantDeserializers.Keys;

    protected readonly __WORD[] _bytes;
    protected readonly int _codeStart;
    protected Atom[] _consts;
    public ReadOnlySpan<__WORD> Code => _bytes.AsSpan(_codeStart);
    public ReadOnlySpan<Atom> Constants => _consts;
    public readonly Dictionary<object, int> ConstantsLookup;
    public readonly Dictionary<__WORD, __WORD> Labels = [];

    public static void RegisterConstantDeserializer(__CONST_TAG tag, ConstantDeserializer handler)
    {
        if (_constantDeserializers.ContainsKey(tag))
            throw new InvalidOperationException($"Deserializer already registered for tag {tag}");
        _constantDeserializers[tag] = handler;
    }

    public static void RegisterConstantSerializer(__CONST_TAG tag, ConstantSerializer handler)
    {
        if (_constantSerializers.ContainsKey(tag))
            throw new InvalidOperationException($"Serializer already registered for tag {tag}");
        _constantSerializers[tag] = handler;
    }

    public static ConstantSerializer GetSerializer(__CONST_TAG tag)
    {
        return _constantSerializers[tag];
    }

    public static ConstantDeserializer GetDeserializer(__CONST_TAG tag)
    {
        return _constantDeserializers[tag];
    }

    static Bytecode()
    {
        RegisterConstantDeserializer(BOOL, DeserializeBool);
        RegisterConstantDeserializer(INT, DeserializeInt);
        RegisterConstantDeserializer(DOUBLE, DeserializeDouble);
        RegisterConstantDeserializer(STRING, DeserializeString);

        RegisterConstantSerializer(BOOL, SerializeBool);
        RegisterConstantSerializer(INT, SerializeInt);
        RegisterConstantSerializer(DOUBLE, SerializeDouble);
        RegisterConstantSerializer(STRING, SerializeString);
    }

    protected Bytecode(__WORD[] bytes, Atom[] constants)
    {
        ReadOnlySpan<__WORD> span = _bytes = bytes;
        _consts = constants;
        ConstantsLookup = _consts.Iterate().ToDictionary(x => x.Item.Value, x => x.Index);
        LoadData(ref span);
        _codeStart = _bytes.Length - span.Length;
    }

    public void SaveTo(FileInfo file)
    {
        if (!file.Directory!.Exists)
            file.Directory.Create();
        using var fs = file.OpenWrite();
        var bytes = new byte[sizeof(__WORD)].AsSpan();
        for (int i = 0; i < _bytes.Length; i += 4)
        {
            bytes[0] = (byte)(_bytes[i] >> 0);
            bytes[1] = (byte)(_bytes[i] >> 8);
            bytes[2] = (byte)(_bytes[i] >> 16);
            bytes[3] = (byte)(_bytes[i] >> 24);
            fs.Write(bytes);
        }
        fs.Flush();
    }

    protected virtual void LoadConstants(ref ReadOnlySpan<__WORD> span)
    {
        var numOfConstants = span[0]; span = span[1..];
        var i = _consts.Length;
        Array.Resize(ref _consts, _consts.Length + numOfConstants);
        for (; i < _consts.Length; i++)
        {
            _consts[i] = DeserializeConstant(ref span);
            ConstantsLookup[_consts[i].Value] = i;
        }
    }

    protected virtual void LoadData(ref ReadOnlySpan<__WORD> span)
    {
        LoadConstants(ref span);
        LoadLabels(ref span);
    }

    protected virtual void LoadLabels(ref ReadOnlySpan<int> span)
    {
        var numOfLabels = span[0]; span = span[1..];
        var ops = new List<Operator>();
        for (int i = 0; i < numOfLabels; i++)
        {
            Labels[span[0]] = span[1];
            span = span[2..];
        }
    }

    protected static Atom DeserializeConstant(ref ReadOnlySpan<__WORD> span)
    {
        var tag = (__CONST_TAG)span[0];

        if (_constantDeserializers.TryGetValue(tag, out var handler))
            return handler(ref span);

        throw new NotSupportedException($"Unknown constant tag: {tag}");
    }

    private static Atom DeserializeBool(ref ReadOnlySpan<__WORD> span)
    {
        var result = (__bool)(span[1] != 0);
        span = span[2..];
        return result;
    }

    private static Atom DeserializeInt(ref ReadOnlySpan<__WORD> span)
    {
        var result = (__int)span[1];
        span = span[2..];
        return result;
    }

    private static Atom DeserializeDouble(ref ReadOnlySpan<__WORD> span)
    {
        var high = (ulong)(uint)span[1];
        var low = (ulong)(uint)span[2];
        var bits = (high << 32) | low;
        var result = (__double)BitConverter.UInt64BitsToDouble(bits);
        span = span[3..];
        return result;
    }

    private static Atom DeserializeString(ref ReadOnlySpan<__WORD> span)
    {
        var lenInWords = span[1];
        var words = span.Slice(2, lenInWords);
        var bytes = new byte[lenInWords * sizeof(__WORD)];

        for (int i = 0; i < lenInWords; ++i)
        {
            bytes[i * 4 + 0] = (byte)(words[i] >> 0);
            bytes[i * 4 + 1] = (byte)(words[i] >> 8);
            bytes[i * 4 + 2] = (byte)(words[i] >> 16);
            bytes[i * 4 + 3] = (byte)(words[i] >> 24);
        }

        span = span[(2 + lenInWords)..];
        return (__string)Encoding.UTF8.GetString(bytes).TrimEnd('\0');
    }

    private static void SerializeBool(Span<__WORD> buffer, Atom value, out int wordsWritten)
    {
        buffer[0] = (int)BOOL;
        buffer[1] = ((bool)value.Value) ? 1 : 0;
        wordsWritten = 2;
    }

    private static void SerializeInt(Span<__WORD> buffer, Atom value, out int wordsWritten)
    {
        buffer[0] = (int)INT;
        buffer[1] = (int)value.Value;
        wordsWritten = 2;
    }

    private static void SerializeDouble(Span<__WORD> buffer, Atom value, out int wordsWritten)
    {
        buffer[0] = (int)DOUBLE;
        ulong bits = BitConverter.DoubleToUInt64Bits((double)value.Value);
        buffer[1] = (int)(bits >> 32);
        buffer[2] = (int)(bits & 0xFFFFFFFF);
        wordsWritten = 3;
    }

    private static void SerializeString(Span<__WORD> buffer, Atom value, out int wordsWritten)
    {
        var bytes = Encoding.UTF8.GetBytes((string)value.Value);
        var len = (bytes.Length + 3) / 4;
        buffer[0] = (int)STRING;
        buffer[1] = len;

        Array.Resize(ref bytes, len * 4); // Pad with \0s
        for (int i = 0; i < len; i++)
            buffer[2 + i] = BitConverter.ToInt32(bytes, i * 4);

        wordsWritten = 2 + len;
    }
}
