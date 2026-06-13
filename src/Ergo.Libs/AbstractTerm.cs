using Ergo.Compiler.Analysis;
using Ergo.Compiler.Emission;
using Ergo.Lang.Parsing;
using Ergo.Runtime.WAM;
using Ergo.Shared.Types;
using AstTerm = Ergo.Lang.Ast.Term;
using ParseDelegate = Ergo.Lang.Parsing.WellKnown.Delegates.Parse;
using EmitGetDelegate = Ergo.Compiler.Emission.WellKnown.Delegates.EmitGet;
using EmitPutDelegate = Ergo.Compiler.Emission.WellKnown.Delegates.EmitPut;
using PrettyDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Pretty;
using GetDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Get;
using UnifyDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Unify;
using PutDelegate = Ergo.Runtime.WAM.WellKnown.Delegates.Put;

namespace Ergo.Libs;

public abstract class AbstractTerm<TAst>(Library parent) : AbstractTerm(parent)
    where TAst : AstTerm
{
    public abstract Func<Maybe<AstTerm>> OnParse(Parser parser);
    public abstract void OnEmitGet(Emitter emitter, EmitterContext ctx, int sig, AstTerm[] args, int Ai, Dictionary<string, int>? varsByName);
    public abstract void OnEmitPut(Emitter emitter, EmitterContext ctx, int sig, AstTerm[] args, int Ai, Dictionary<string, int>? varsByName, bool deep);
    public abstract void OnUnify(ErgoVM vm, int addr1, int addr2, Stack<(int, int)> todo);
    public abstract AstTerm OnGet(ErgoVM vm, int addr);
    public abstract int OnPut(ErgoVM vm, TAst term);
    public abstract string OnPretty(ErgoVM vm, int addr, bool quoted);

    public sealed override Type AstType => typeof(TAst);

    public sealed override Delegate Parse => (ParseDelegate)(p => OnParse(p)!);
    public sealed override Delegate EmitGet => (EmitGetDelegate)OnEmitGet;
    public sealed override Delegate EmitPut => (EmitPutDelegate)OnEmitPut;
    public sealed override Delegate Unify => (UnifyDelegate)OnUnify;
    public sealed override Delegate Get => (GetDelegate)OnGet;
    public sealed override Delegate Put => (PutDelegate)((vm, term) => OnPut(vm, (TAst)term));
    public sealed override Delegate Pretty => (PrettyDelegate)OnPretty;
}
