using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public class DictParseTests : Tests
{
    [Fact] public void Parse_Dict_In_Query()
        => Assert.Single(new ErgoVM().findall(CompileQuery(Consult("list_tests"), "X = event{type: click}")));

    [Fact] public void Parse_Dict_Fact_And_Query()
    {
        var s = new ErgoVM().findall(CompileQuery(Consult("dict_tests"), "contento(X, H)"));
        Assert.NotEmpty(s);
        Assert.Equal("fox", s[0].Bindings[0].Value.Expl);
        Assert.Equal("postgres", s[0].Bindings[1].Value.Expl);
    }
}
