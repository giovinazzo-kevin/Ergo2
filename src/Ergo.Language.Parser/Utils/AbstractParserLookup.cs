using Ergo.Language.Ast;

namespace Ergo.Language.Parser;

public record class AbstractParserLookup(Dictionary<Type, List<Operator>> Table)
{
}
