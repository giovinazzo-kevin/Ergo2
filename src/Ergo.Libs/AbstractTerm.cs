using Ergo.Lang.Ast;
using Ergo.Shared.Interfaces;

namespace Ergo.Libs;

/// <summary>
/// An abstract term is a library-defined term type with custom 
/// compilation, unification, parsing, and printing behavior.
/// The canonical form on the heap is owned by the implementor.
/// Registered by signature (e.g. [|]/2 for lists, {|}/2 for dicts).
/// </summary>
public abstract class AbstractTerm
{
    public abstract Signature Signature { get; }
    public abstract void EmitRead(IEmitterContext ctx, int Ai);
    public abstract void EmitWrite(IEmitterContext ctx, int Ai);
    public abstract bool Unify(IHeapAccess heap, int addr1, int addr2);
    public abstract Term Read(IHeapAccess heap, int addr);
}
