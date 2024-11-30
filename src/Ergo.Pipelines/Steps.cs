using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Pipelines.Analysis;
using Ergo.Pipelines.Compiler;
using Ergo.Pipelines.IO;
using Ergo.Shared.Types;

namespace Ergo.Pipelines;

public static class Steps
{
    public static readonly LoadSource LoadSource = LoadSource.Instance;
    public static readonly Analyze Analyze = Analyze.Instance;
    public static readonly Compile Compile = Compile.Instance;
}
