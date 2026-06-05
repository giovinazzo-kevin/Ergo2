using Ergo.Compiler.Analysis;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Shared.Types;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ergo.Compiler.Emission;

public sealed partial class KnowledgeBase
{
    public readonly string Name;
    public readonly KnowledgeBaseBytecode Bytecode;
    public readonly List<Delegate> BuiltInHandlers = [];

    public KnowledgeBase(ErgoFileStream file)
        : this(file.Name, ReadFile(file)) { }

    public KnowledgeBase(string name, KnowledgeBaseBytecode bytecode)
    {
        Name = name;
        Bytecode = bytecode;
    }

    static KnowledgeBaseBytecode ReadFile(ErgoFileStream file)
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
        var n = arity.TryGetValue(out var a) ? a : (int)Emission.Signature.VARIADIC;
        var sig = (Signature)(c, n);
        var idx = _nextBuiltInIdx++;
        Bytecode.Labels[sig] = -(idx + 1);
        while (BuiltInHandlers.Count <= idx) BuiltInHandlers.Add(null!);
        BuiltInHandlers[idx] = handler;
        return idx;
    }

    public int RegisterBuiltInLabel(string name, int arity, Delegate handler)
        => RegisterBuiltInLabel(name, (Maybe<int>)arity, handler);

    public Query Query(string query)
    {
        var file = ErgoFileStream.Create(query, nameof(Query));
        using var lexer = new Lexer(file, Bytecode.Operators);
        using var parser = new Parser(lexer);
        if (!parser.BinaryExpressionRhs().TryGetValue(out var ast))
            throw new InvalidOperationException();
        var emitter = new Emitter();
        var q = emitter.Query(ast, Bytecode);
        return q with { BuiltInHandlers = BuiltInHandlers, Source = this };
    }
}
