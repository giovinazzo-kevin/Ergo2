using Ergo.Compiler.Analysis;
using Ergo.Libs.Prologue.BuiltIns;
using Ergo.Libs.Prologue.Directives;

namespace Ergo.Libs.Prologue;

public sealed class LibPrologue : Library
{
    public LibPrologue(Module parent) : base(parent)
    {
        ExportedDirectives = [
            new DeclareModule(this),
            new UseModule(this),
            new DeclareOperator(this),
            new DeclareDynamic(this)
        ];
        ExportedBuiltIns = [
            new Assert(this),
            new AssertZ(this),
            new AssertA(this),
            new Retract(this)
        ];
    }
}
