using Ergo.Compiler.Analysis;

namespace Ergo.Libs.List.Lib;

public sealed class List : Library
{
    public List(Module parent) : base(parent)
    {
        ExportedAbstractTerms = [new Abs.List(this)];
    }
}
