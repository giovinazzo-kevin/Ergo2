using Ergo.Compiler.Analysis;
using Ergo.Libs.Prologue.BuiltIns;

namespace Ergo.Libs.Prologue.Lib;

public sealed class Prologue : Library
{
    public Prologue(Module parent) : base(parent)
    {
        ExportedBuiltIns = [
            new Assert(this),
            new AssertZ(this),
            new AssertA(this),
            new Retract(this)
        ];
    }
}
