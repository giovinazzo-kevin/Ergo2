using Ergo.Compiler.Analysis;
using Ergo.Libs.IO.BuiltIns;

namespace Ergo.Libs.IO.Lib;

public sealed class IO : Library
{
    public IO(Module parent) : base(parent)
    {
        ExportedBuiltIns = [
            new Write(this),
            new Nl(this),
            new WriteLn(this)
        ];
    }
}
