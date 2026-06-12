using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.IO.BuiltIns;

public sealed class Nl(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new((__string)"nl", 0);
    public override ErgoVM.__op Handle => vm => {
        vm.Out.WriteLine();
    };
}
