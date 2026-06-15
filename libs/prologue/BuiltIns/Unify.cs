using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.Prologue.BuiltIns;

public sealed class Unify(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new((__string)"=", 2);
    public override ErgoVM.__op Handle => vm =>
    {
        var a = vm.deref(ErgoVM.arg_addr(0));
        var b = vm.deref(ErgoVM.arg_addr(1));
        vm.unify(a, b);
    };
}
