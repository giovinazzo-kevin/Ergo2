using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.Prologue.BuiltIns;

public sealed class Retract(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new(new __string("retract"), 1);
    public override Delegate Handler => (ErgoVM.__op)(vm => vm.RetractClause(0));
}
