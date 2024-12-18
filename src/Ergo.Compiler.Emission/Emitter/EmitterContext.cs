﻿using Ergo.Lang.Lexing;
using Microsoft.VisualBasic;
using System.Text;

namespace Ergo.Compiler.Emission;

public sealed class EmitterContext
{
    private Dictionary<__WORD, __WORD> _labels = [];
    private List<__WORD> _constants = [];
    private Dictionary<object, __WORD> _constantLookup = [];
    private readonly List<Op> _instructions = [];
    private readonly OperatorLookup _operators;

    public int PC { get; private set; }
    public int NumVars { get; set; } = 0;

    internal EmitterContext(OperatorLookup? operators = null) => _operators = operators ?? new ();
    public EmitterContext Scope() => new(_operators)
    {
        _constants = _constants,
        _constantLookup = _constantLookup,
        _labels = _labels
    };
    public void Emit(Op op)
    {
        _instructions.Add(op);
        PC += op.Size;
    }
    public void Concat(EmitterContext other)
    {
        _instructions.AddRange(other._instructions);
        PC += other.PC;
    }
    public void Label(Signature sig, __WORD address)
    {
        _labels[sig] = address;
    }
    public int Constant(object value)
    {
        if (_constantLookup.TryGetValue(value, out var result)) 
            return result;
        var j = _constantLookup[value] = _constantLookup.Count;
        _constants.Add(value.GetType().GetHashCode());
        switch (value)
        {
            case bool __bool:
                _constants.Add(__bool ? 1 : 0);
                break;
            case int __int:
                _constants.Add(__int);
                break;
            case double __double:
                var bits = BitConverter.DoubleToInt64Bits(__double);
                _constants.Add((__WORD)(bits & 0xFFFFFFFF));
                _constants.Add((__WORD)((bits >> 32) & 0xFFFFFFFF));
                break;
            case string __string:
                var bytes = Encoding.UTF8.GetBytes(__string);
                var padding = (sizeof(__WORD) - bytes.Length % sizeof(__WORD)) % sizeof(__WORD);
                Array.Resize(ref bytes, bytes.Length + padding);
                var span = bytes.AsSpan();
                var lenInWords = (__WORD)Math.Ceiling((double)span.Length / sizeof(__WORD));
                _constants.Add(lenInWords);
                for (int i = 0; i < lenInWords; i++)
                {
                    var start = i * sizeof(__WORD);
                    var end = Math.Min(span.Length, start + sizeof(__WORD));
                    var word = BitConverter.ToInt32(span[start..end]);
                    _constants.Add(word);
                }
                break;
            default:
                throw new NotSupportedException();
        }
        return j;
    }
    public KnowledgeBaseBytecode ToKnowledgeBase()
    {
        var labelsLength = _labels.Values.Count * 2 ;
        var operatorsLength = _operators.Values.Sum(x => x.Functors.Length + 3);
        var instructionsLength = _instructions.Sum(x => x.Size);
        var data = new __WORD[instructionsLength + operatorsLength + labelsLength + 2];
        var span = data.AsSpan();
        span[0] = _labels.Values.Count; span = span[1..];
        foreach (var label in _labels)
        {
            (span[0], span[1]) = (label.Key, label.Value);
            span = span[2..];
        }
        span[0] = _operators.Values.Count; span = span[1..];
        foreach (var op in _operators.Values)
        {
            span[0] = op.Functors.Length;
            span[1] = op.Precedence;
            span[2] = (__WORD)op.Type_;
            for (int i = 0; i < op.Functors.Length; i++)
                span[3 + i] = Constant(op.Functors[i].Value);
            span = span[(op.Functors.Length + 3)..];
        }
        for (int i = 0; i < _instructions.Count; i++)
            _instructions[i].Emit(ref span);
        Array.Resize(ref data, data.Length + _constants.Count + 1);
        Array.Copy(data, 0, data, _constants.Count + 1, data.Length - _constants.Count - 1);
        span = data.AsSpan();
        span[0] = _constantLookup.Count;
        _constants.CopyTo(span[1..]);
        return new(data);
    }
    public QueryBytecode ToQuery(KnowledgeBaseBytecode kb)
    {
        var instructionsLength = _instructions.Sum(x => x.Size);
        var data = new __WORD[kb.Labels.Count * 2 + 1 + kb.Code.Length + instructionsLength];
        var span = data.AsSpan();
        span[0] = kb.Labels.Count; span = span[1..];
        foreach(var label in kb.Labels)
        {
            span[0] = label.Key;
            span[1] = label.Value;
            span = span[2..];
        }
        kb.Code.CopyTo(span); 
        span = span[kb.Code.Length..];
        for (int i = 0; i < _instructions.Count; i++)
            _instructions[i].Emit(ref span);
        Array.Resize(ref data, data.Length + _constants.Count + 1);
        Array.Copy(data, 0, data, _constants.Count + 1, data.Length - _constants.Count - 1);
        span = data.AsSpan();
        span[0] = _constantLookup.Count;
        _constants.CopyTo(span[1..]);
        return new(data, kb.Constants.ToArray(), kb.Code.Length);
    }
}
