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
    public readonly string Name;
    public readonly KnowledgeBaseBytecode Bytecode;

    public KnowledgeBase(ErgoFileStream file)
        : this(file.Name, ReadFile(file)) { }

    public KnowledgeBase(string name, KnowledgeBaseBytecode bytecode)
    {
        Name = name;
        Bytecode = bytecode;
    }

    static KnowledgeBaseBytecode ReadFile(ErgoFileStream file)
    {
        return new([]);
    }

    private int _nextBuiltInIdx = 0;

    public int RegisterBuiltInLabel(string name, int arity)
    {
        var c = Bytecode.AddConstant(new Lang.Ast.__string(name));
        var sig = (Signature)(c, arity);
        var idx = _nextBuiltInIdx++;
        Bytecode.Labels[sig] = -(idx + 1);
        return idx;
    }

    public Query Query(string query)
    {
        var file = ErgoFileStream.Create(query, nameof(Query));
        using var lexer = new Lexer(file, Bytecode.Operators);
        using var parser = new Parser(lexer);
        if (!parser.BinaryExpressionRhs().TryGetValue(out var ast))
            throw new InvalidOperationException();
        var emitter = new Emitter();
        return emitter.Query(ast, Bytecode);
    }
}
