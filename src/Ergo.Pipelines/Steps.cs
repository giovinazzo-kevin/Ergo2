using Ergo.Pipelines.Analysis;
using Ergo.Pipelines.Compiler;
using Ergo.Pipelines.IO;

namespace Ergo.Pipelines;

public static class Steps
{
    public static readonly LoadSource LoadSource = LoadSource.Instance;
    public static readonly Analyze Analyze = Analyze.Instance;
    public static readonly Compile Compile = Compile.Instance;
    public static readonly Consult Consult = Consult.Instance;
}
