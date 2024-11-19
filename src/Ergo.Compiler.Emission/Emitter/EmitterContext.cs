using Ergo.Lang.Lexing;
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
    internal EmitterContext(OperatorLookup operators) => _operators = operators;
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
    public void EmitMany(EmitterContext other)
    {
        _instructions.AddRange(other._instructions);
        other._instructions.Clear();
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
    public __WORD[] ToArray()
    {
        var operatorsLength = _operators.Values.Sum(x => x.Functors.Length + 3);
        var instructionsLength = _instructions.Sum(x => x.Size);
        var data = new __WORD[instructionsLength + operatorsLength + 1];
        var span = data.AsSpan();
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
        Array.Copy(data, 0, data, _constants.Count + 1, instructionsLength + operatorsLength + 1);
        span = data.AsSpan();
        span[0] = _constantLookup.Count;
        _constants.CopyTo(span[1..]);
        return data;
    }
}
