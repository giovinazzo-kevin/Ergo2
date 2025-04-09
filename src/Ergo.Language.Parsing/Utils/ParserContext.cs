using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Shared.Types;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ergo.Lang.Parsing;

public class ParserContext
{
    private int _lastFreeVar = 0;
    private readonly Stack<Block> _blocks = [];
    private readonly HashSet<(long, string)> _failures = [];
    private readonly Stack<TraceNode> _traceStack = new();
    public int ParseDepth => _traceStack.Count;
    public TraceNode ParseRoot { get; } = new() { Method = "Root", Value = "Parse" };
    public Maybe<Block> CurrentBlock => _blocks.Count == 0 ? Maybe.None<Block>() : _blocks.Peek();

    public void MemoizeFailure(LexerState state, [CallerMemberName] string parser = "") =>
        _failures.Add((state.StreamPos, parser));

    public bool IsFailureMemoized(LexerState state, [CallerMemberName] string parser = "") =>
        _failures.Contains((state.StreamPos, parser));

    public Variable GetVariable(string name)
    {
        if (!CurrentBlock.TryGetValue(out var block))
            return new Variable(name);

        if (block.Variables.TryGetValue(name, out var v))
            return v;

        if (name == Literals.Discard.Name)
            name = $"__{Interlocked.Increment(ref _lastFreeVar)}";

        return block.Variables[name] = new Variable(name);
    }

    public void EnterBlock()
    {
        _blocks.Push(Block.Empty);
    }

    public void ExitBlock()
    {
        _blocks.Pop();
    }


    [Conditional("PARSER_TRACE")]
    public void EnterParse()
    {
        var node = new TraceNode { Method = "", Value = "" };
        if (_traceStack.TryPeek(out var parent))
        {
            node.Parent = parent;
            parent.Children.Add(node);
        }
        else
        {
            ParseRoot.Children.Add(node);
            node.Parent = ParseRoot;
        }
        _traceStack.Push(node);
    }

    [Conditional("PARSER_TRACE")]
    public void ExitParse()
    {
        _traceStack.TryPop(out var _);
    }

    [Conditional("PARSER_TRACE")]
    public void AnnotateLastNode(string method, string value, bool success = true)
    {
        if (_traceStack.TryPeek(out var node))
        {
            node.Method = method;
            node.Value = value;
            node.Success = success;
        }
    }


}
