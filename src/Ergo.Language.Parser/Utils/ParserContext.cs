using Ergo.Language.Ast;
using Ergo.Language.Ast.WellKnown;
using Ergo.Language.Lexer;
using Ergo.Shared.Types;

namespace Ergo.Language.Parser;


public class ParserContext
{
    private int _lastIgnoredVarId = 0;
    
    private readonly Stack<Block> _blocks = [];
    public Maybe<Block> CurrentBlock => _blocks.Count == 0 ? Maybe.None<Block>() : _blocks.Peek();
    public Variable GetVariable(string name)
    {
        if (!CurrentBlock.TryGetValue(out var block))
            return new Variable(name);
        if (block.Variables.TryGetValue(name, out var v))
            return v;
        if (name == Literals.Discard.Name)
            name = $"__{Interlocked.Increment(ref _lastIgnoredVarId)}";
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
