using Ergo.Compiler.Analysis;
using Ergo.Libs.Stdlib.Directives;

namespace Ergo.Libs.Stdlib;

public sealed class LibStdlib : Library
{
    public LibStdlib(Module parent) : base(parent)
    {
        ExportedDirectives = [
            new DeclareModule(this),
            new DeclareModuleSimple(this),
            new UseModule(this),
            new DeclareOperator(this),
            new DeclareDynamic(this)
        ];
    }
}
