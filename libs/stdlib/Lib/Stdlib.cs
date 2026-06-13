using Ergo.Compiler.Analysis;
using Ergo.Libs.Stdlib.Directives;

namespace Ergo.Libs.Stdlib.Lib;

public sealed class Stdlib : Library
{
    public Stdlib(Module parent) : base(parent)
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
