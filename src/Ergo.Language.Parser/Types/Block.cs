using Ergo.Language.Ast;

namespace Ergo.Language.Parser;

public readonly record struct Block(Dictionary<string, Variable> Variables)
{
    public static Block Empty => new([]);
}
