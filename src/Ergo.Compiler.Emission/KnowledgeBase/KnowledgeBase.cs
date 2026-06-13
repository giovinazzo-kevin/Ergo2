using Ergo.IO;
using Ergo.Shared.Types;

namespace Ergo.Compiler.Emission;

public sealed partial class KnowledgeBase
{
    public readonly string Name;
    public readonly KnowledgeBaseBytecode Bytecode;
    public readonly List<Delegate> BuiltInHandlers = [];
    public readonly Dictionary<__WORD, Analysis.AbstractTerm> AbstractTerms = [];
    public readonly Dictionary<(object Functor, int Arity), Func<Lang.Ast.Term[], Lang.Ast.Term>> Reconstructors = [];

    public KnowledgeBase(ErgoFileStream file)
        : this(file.Name, ReadFile(file)) { }

    public KnowledgeBase(string name, KnowledgeBaseBytecode bytecode)
    {
        Name = name;
        Bytecode = bytecode;
    }

    private static KnowledgeBaseBytecode ReadFile(ErgoFileStream file)
    {
        using var ms = new MemoryStream();
        file.Stream.CopyTo(ms);
        var raw = ms.ToArray();
        var words = new __WORD[raw.Length / sizeof(__WORD)];
        for (int i = 0; i < words.Length; i++)
            words[i] = BitConverter.ToInt32(raw, i * 4);
        return new KnowledgeBaseBytecode(words);
    }

    private int _nextBuiltInIdx = 0;

    public int RegisterBuiltInLabel(string name, Maybe<int> arity, Delegate handler)
    {
        var c = Bytecode.AddConstant(new Lang.Ast.__string(name));
        var n = arity.TryGetValue(out var a) ? a : Signature.VARIADIC;
        var sig = (Signature)(c, n);
        var idx = _nextBuiltInIdx++;
        Bytecode.Labels[sig] = -(idx + 1);
        while (BuiltInHandlers.Count <= idx) BuiltInHandlers.Add(null!);
        BuiltInHandlers[idx] = handler;
        return idx;
    }

    public int RegisterBuiltInLabel(string name, int arity, Delegate handler)
        => RegisterBuiltInLabel(name, (Maybe<int>)arity, handler);

    public void RegisterAbstractTerm(Analysis.AbstractTerm abs)
    {
        var c = Bytecode.AddConstant(new Lang.Ast.__string((string)abs.Signature.Functor.Value));
        var n = abs.Signature.Arity.TryGetValue(out var a) ? a : Signature.VARIADIC;
        var packed = (Signature)(c, n);
        AbstractTerms[packed] = abs;
    }

}
