using Ergo.Compiler.Analysis;
using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;
using Ergo.Shared.Types;
using Signature = Ergo.Lang.Ast.Signature;

namespace Ergo.Libs.IO.BuiltIns;

public sealed class Write(Library parent) : BuiltIn(parent)
{
    public override Signature Signature { get; } = new((__string)"write", default(Maybe<int>));
    public override ErgoVM.__op Handle => vm => {
        for (int i = 0; i < vm.N; i++)
            vm.Out.Write(vm.Pretty(vm.A[i]));
    };
}
