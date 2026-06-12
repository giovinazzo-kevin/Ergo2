using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;

namespace Ergo.Libs;

public abstract class AbstractTerm<TAst>(Library parent) : Ergo.Compiler.Analysis.AbstractTerm(parent)
    where TAst : Lang.Ast.Term
{
    public abstract void OnUnify(ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo);
    public abstract Lang.Ast.Term OnRead(ErgoVM vm, int addr);
    public abstract int OnWriteHeap(ErgoVM vm, Lang.Ast.Term term);
    public abstract string OnPretty(ErgoVM vm, int addr, bool quoted);

    public sealed override Delegate Unify => (ErgoVM.__abs_unify)OnUnify;
    public sealed override Delegate Read => (ErgoVM.__abs_read)OnRead;
    public sealed override Delegate WriteHeap => (ErgoVM.__abs_write_heap)OnWriteHeap;
    public sealed override Delegate Pretty => (ErgoVM.__abs_pretty)OnPretty;
}
