namespace Ergo.Compiler.Analysis;

public abstract class Library(Module parent) : CallGraph.Node<Module>
{
    public override Module Parent => parent;
    public IEnumerable<Directive> ExportedDirectives { get; init; } = [];
    public IEnumerable<BuiltIn> ExportedBuiltIns { get; init; } = [];
}
