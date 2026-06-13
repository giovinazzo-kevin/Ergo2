using Ergo.Compiler.Analysis;

namespace Ergo.Libs.Lists;

public sealed class LibList : Library
{
    public LibList(Module parent) : base(parent)
    {
        ExportedAbstractTerms = [new List(this)];
    }
}
