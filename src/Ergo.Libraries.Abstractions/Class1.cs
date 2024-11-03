using Ergo.AST;

namespace Ergo.Libraries.Abstractions;

public interface IExportsDirective<T> where T : ErgoDirective;
public interface IExportsBuiltIn<T> where T : ErgoBuiltIn;

// see https://github.com/G3Kappa/Ergo/issues/10
public interface IErgoLibrary
{
    int LoadOrder { get; }
    Atom Module { get; }
    IEnumerable<ErgoDirective> ExportedDirectives { get; }
    IEnumerable<ErgoBuiltIn> ExportedBuiltins { get; }
    void OnErgoEvent(ErgoEvent evt);
}