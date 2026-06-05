using Ergo.Compiler.Analysis;
using Ergo.Libs.Prologue.BuiltIns;

namespace Ergo.Libs.Prologue;

public sealed class LibPrologue : Library
{
    public LibPrologue(Module parent) : base(parent)
    {
        ExportedBuiltIns = [
            new Assert(this),
            new AssertZ(this),
            new AssertA(this),
            new Retract(this)
        ];
    }
}
