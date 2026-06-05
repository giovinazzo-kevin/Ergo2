using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;

namespace Ergo.Libs.Prologue.BuiltIns;

public sealed class Assert(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new(new __string("assert"), 1);
    public override Delegate Handler => (ErgoVM.__op)(vm => vm.AssertClause(0, atEnd: true));
}

public sealed class AssertZ(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new(new __string("assertz"), 1);
    public override Delegate Handler => (ErgoVM.__op)(vm => vm.AssertClause(0, atEnd: true));
}

public sealed class AssertA(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new(new __string("asserta"), 1);
    public override Delegate Handler => (ErgoVM.__op)(vm => vm.AssertClause(0, atEnd: false));
}

public sealed class Retract(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new(new __string("retract"), 1);
    public override Delegate Handler => (ErgoVM.__op)(vm => vm.RetractClause(0));
}
