﻿using Ergo.IO;
using Ergo.Lang.Ast;
using System;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ergo.Compiler.Emission;

public abstract class Bytecode
{
    private static readonly int HASH_BOOL = typeof(bool).GetHashCode();
    private static readonly int HASH_INT32 = typeof(int).GetHashCode();
    private static readonly int HASH_DOUBLE = typeof(double).GetHashCode();
    private static readonly int HASH_STRING = typeof(string).GetHashCode();

    private readonly __WORD[] _bytes;
    private readonly int _codeStart;

    protected Atom[] Consts { get; private set; } = [];

    public ReadOnlySpan<__WORD> Code => _bytes.AsSpan(_codeStart);
    public ReadOnlySpan<Atom> Constants => Consts;

    protected Bytecode(__WORD[] bytes)
    {
        ReadOnlySpan<__WORD> span = _bytes = bytes;
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
        Consts = new Atom[numOfConstants];
        for (int i = 0; i < numOfConstants; i++)
            Consts[i] = DeserializeConstant(ref span);
    }

    protected virtual void LoadData(ref ReadOnlySpan<__WORD> span)
    {
        LoadConstants(ref span);
    }

    protected static Atom DeserializeConstant(ref ReadOnlySpan<__WORD> span)
    {
        var type = span[0];
        if (type == HASH_BOOL)
        {
            var asBool = span[1] != 0;
            span = span[2..];
            return (__bool)asBool;
        }
        if (type == HASH_INT32)
        {
            var asInt = span[1];
            span = span[2..];
            return (__int)asInt;
        }
        if (type == HASH_DOUBLE)
        {
            var asDouble = BitConverter.UInt64BitsToDouble((ulong)(span[1] << 32) | (ulong)span[2]);
            span = span[3..];
            return (__double)asDouble;
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
            return (__string)Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
        throw new NotSupportedException();
    }
}
