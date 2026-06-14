using Ergo.Compiler.Analysis;

namespace Ergo.Libs.Dict.Lib;

public sealed class Dict : Library
{
    public Dict(Module parent) : base(parent)
    {
        ExportedAbstractTerms = [new Abs.Dict(this)];
    }
}
