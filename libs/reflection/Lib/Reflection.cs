using Ergo.Compiler.Analysis;

namespace Ergo.Libs.Reflection.Lib;

public sealed class Reflection : Library
{
    public Reflection(Module parent) : base(parent)
    {
        ExportedBuiltIns = [
            new BuiltIns.TermBuiltIn(this)
        ];
    }
}
