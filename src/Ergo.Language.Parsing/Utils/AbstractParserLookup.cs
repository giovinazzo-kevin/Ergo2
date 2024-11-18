using Ergo.Lang.Ast;

namespace Ergo.Lang.Parsing;

public record class AbstractParserLookup(Dictionary<Type, List<Operator>> Table)
{
}
