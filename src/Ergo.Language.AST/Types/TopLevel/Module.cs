using Ergo.Shared.Interfaces;

namespace Ergo.Language.Ast;

public class Module(IEnumerable<Directive> directives, IEnumerable<Clause> clauses) : IExplainable
{
    public readonly Directive[] Directives = [..directives];
    public readonly Clause[] Clauses = [.. clauses];

    public string Expl =>
        string.Join("", Directives.Select(x => x.Expl + ".\n"))
        + "\n"
        + string.Join("", Clauses.Select(x => x.Expl + ".\n"));
}