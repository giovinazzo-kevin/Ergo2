using Ergo.Compiler.Analysis;
using Ergo.Libs.IO.BuiltIns;

namespace Ergo.Libs.IO;

public sealed class LibIO : Library
{
    public LibIO(Module parent) : base(parent)
    {
        ExportedBuiltIns = [
            new Write(this),
            new Nl(this),
            new WriteLn(this)
        ];
    }
}
