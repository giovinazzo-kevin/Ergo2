using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.Prologue.BuiltIns;

public sealed class AssertA(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new((__string)"asserta", 1);
    public override ErgoVM.__op Handle => vm => vm.AssertClause(0, atEnd: false);
}
