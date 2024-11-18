using Ergo.Lang.Ast;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Shared.Types;
using System.Runtime.CompilerServices;

namespace Ergo.Lang.Parsing;


public class ParserContext
{
    private int _lastFreeVar = 0;
    private readonly Stack<Block> _blocks = [];
    public Maybe<Block> CurrentBlock => _blocks.Count == 0 ? Maybe.None<Block>() : _blocks.Peek();
    private readonly HashSet<(long, string)> _failures = [];
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
}
