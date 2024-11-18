using Ergo.Lang.Ast;

namespace Ergo.Compiler.Analysis;

public class Predicate(Module parent, Signature sig) : CallGraph.Node<Module>
{
    public readonly Signature Signature = sig;
    public override Module Parent => parent;
    public readonly List<BuiltIn> BuiltIns = [];
    public readonly List<Clause> Clauses = [];
}
