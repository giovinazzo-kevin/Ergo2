using Ergo.Compiler.Analysis;
using Ergo.Runtime.WAM;

namespace Ergo.Libs;

public abstract class BuiltIn(Library parent) : Compiler.Analysis.BuiltIn(parent)
{
    public abstract ErgoVM.__op Handle { get; }
    public sealed override Delegate Handler => Handle;
}
