using Ergo.Language.Ast;

namespace Ergo.Language.Parsing;

public record class AbstractParserLookup(Dictionary<Type, List<Operator>> Table)
{
}
