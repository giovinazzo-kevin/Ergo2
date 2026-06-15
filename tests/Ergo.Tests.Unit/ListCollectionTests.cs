using Ergo.Lang.Ast;
using Ergo.Libs.List.Ast;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class ListCollectionTests : CollectionTests<List>
{
    protected override string Module => "list_tests";
    protected override string OpenDelim => "[";
    protected override string CloseDelim => "]";
    protected override Term EmptyElement => Ergo.Libs.List.WellKnown.EmptyList;
    protected override List MakeCollection(Term[] elements, Term tail) => new(elements, tail);
    protected override List MakeEmpty() => new([], Ergo.Libs.List.WellKnown.EmptyList);

    [Theory]
    [InlineData("append([], [a, b], X)", new[] { "X/[a, b]" })]
    [InlineData("append([a, b], [], X)", new[] { "X/[a, b]" })]
    [InlineData("append([a], [b, c], X)", new[] { "X/[a, b, c]" })]
    [InlineData("append([], [], X)", new[] { "X/[]" })]
    [InlineData("member(X, [a, b, c])", new[] { "X/a", "X/b", "X/c" })]
    public void Query(string query, string[] expected)
        => AssertSolutions(new ErgoVM().findall(CompileQuery(Consult(Module), query)), expected);

    [Theory]
    [InlineData("member(b, [a, b, c])", 1)]
    [InlineData("member(d, [a, b, c])", 0)]
    public void Ground(string query, int expected)
        => Assert.Equal(expected, new ErgoVM().findall(CompileQuery(Consult(Module), query)).Count);
}
