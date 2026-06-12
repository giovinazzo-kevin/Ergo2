using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Lang.Parsing;
using Ergo.Runtime.WAM;
using Ergo.Shared.Types;
using ParseDelegate = Ergo.Lang.Parsing.WellKnown.Delegates.Parse;
using UnifyDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Unify;
using ReadDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Read;
using WriteHeapDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.WriteHeap;
using PrettyDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Pretty;

namespace Ergo.Libs;

public abstract class AbstractTerm<TAst>(Library parent) : Ergo.Compiler.Analysis.AbstractTerm(parent)
    where TAst : Lang.Ast.Term
{
    public abstract Func<Maybe<Term>> OnParse(Parser parser);
    public abstract void OnUnify(ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo);
    public abstract Term OnRead(ErgoVM vm, int addr);
    public abstract int OnWriteHeap(ErgoVM vm, Term term);
    public abstract string OnPretty(ErgoVM vm, int addr, bool quoted);

    public sealed override Delegate Parse => (ParseDelegate)(p => OnParse(p)!);
    public sealed override Delegate Unify => (UnifyDelegate)OnUnify;
    public sealed override Delegate Read => (ReadDelegate)OnRead;
    public sealed override Delegate WriteHeap => (WriteHeapDelegate)OnWriteHeap;
    public sealed override Delegate Pretty => (PrettyDelegate)OnPretty;
}
