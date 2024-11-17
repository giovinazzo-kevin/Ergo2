using Ergo.Compiler.Analysis;
using Ergo.Libs.Prologue.Directives;
namespace Ergo.Libs.Prologue;

public sealed class LibPrologue : Library
{
    public LibPrologue(Module parent) : base(parent)
    {
        ExportedDirectives = [
            new DeclareModule(this),
            new UseModule(this),
            new DeclareOperator(this)
        ];
    }
}
