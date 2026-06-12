using Ergo.Compiler.Analysis.Exceptions;
using Ergo.Lang.Ast;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;

namespace Ergo.Compiler.Analysis;

public class Module(CallGraph parent, __string name) : CallGraph.Node<CallGraph>
{
    public enum Stage : int
    {
        Unloaded = 0,
        Linked = 1,
        Imported = 2,
        Opened = 3,
        Preloaded = 4,
        Loaded = 5
    }

    private __string _name = name;
    public __string Name {
        get => _name;
        set {
            if (LoadStage < Stage.Loaded)
                _name = value;
            else
                throw new AnalyzerException(AnalyzerError.CannotRedefineModule0As1When2, _name, value, "after it has loaded");
        }
    }
    public override CallGraph Parent => parent;
    public readonly List<Module> Imports = [];
    public readonly List<Library> Libraries = [];
    public readonly Dictionary<Signature, Predicate> Predicates = [];
    public readonly HashSet<Signature> Exports = [];
    public readonly HashSet<Signature> Dynamic = [];
    public readonly List<Ergo.Lang.Parsing.WellKnown.Delegates.Parse> AbstractParsers = [];

    public Stage LoadStage { get; internal set; }
    public bool IsLoading { get; internal set; }
    internal Parser? _parser { get; set; } = null;
}
