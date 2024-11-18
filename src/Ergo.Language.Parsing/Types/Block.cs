using Ergo.Lang.Ast;

namespace Ergo.Lang.Parsing;

public readonly record struct Block(Dictionary<string, Variable> Variables)
{
    public static Block Empty => new([]);
}
