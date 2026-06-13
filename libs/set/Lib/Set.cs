using Ergo.Compiler.Analysis;

namespace Ergo.Libs.Set.Lib;

public sealed class Set : Library
{
    public Set(Module parent) : base(parent)
    {
        ExportedAbstractTerms = [new Abs.Set(this)];
    }
}
