using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.Prologue.BuiltIns;

public sealed class AssertZ(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new(new __string("assertz"), 1);
    public override Delegate Handler => (ErgoVM.__op)(vm => vm.AssertClause(0, atEnd: true));
}
