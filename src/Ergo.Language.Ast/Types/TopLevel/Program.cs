using Ergo.Shared.Interfaces;
using System.Security.Cryptography.X509Certificates;

namespace Ergo.Language.Ast;

public class Program(IEnumerable<Directive> directives, IEnumerable<Clause> clauses) : IExplainable
{
    public readonly Directive[] Directives = [..directives];
    public readonly Clause[] Clauses = [.. clauses];

    public string Expl =>
        string.Join("", Directives.Select(x => x.Expl + ".\n"))
        + "\n"
        + string.Join("", Clauses.Select(x => x.Expl + ".\n"));
}