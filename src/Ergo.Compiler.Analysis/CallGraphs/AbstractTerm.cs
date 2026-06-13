using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public abstract class AbstractTerm(Library parent) : CallGraph.Node<Library>
{
    public override Library Parent => parent;
    public abstract Signature Signature { get; }
    public abstract Delegate Parse { get; }
    public abstract Delegate EmitGet { get; }
    public abstract Delegate EmitPut { get; }
    public abstract Delegate Unify { get; }
    public abstract Delegate Get { get; }
    public abstract Delegate Put { get; }
    public abstract Delegate Pretty { get; }
    public abstract Type AstType { get; }
    public int PackedSig { get; set; }
}
