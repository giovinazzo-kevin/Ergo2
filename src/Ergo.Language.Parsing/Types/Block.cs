using Ergo.Language.Ast;

namespace Ergo.Language.Parsing;

public readonly record struct Block(Dictionary<string, Variable> Variables)
{
    public static Block Empty => new([]);
}
