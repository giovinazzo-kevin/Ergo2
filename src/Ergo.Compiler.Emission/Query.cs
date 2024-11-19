using Ergo.Compiler.Analysis;

namespace Ergo.Compiler.Emission;

public record Query(ReadOnlyMemory<__WORD> Code, int Start)
{
}
