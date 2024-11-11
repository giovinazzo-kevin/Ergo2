using Ergo.IO;
using Ergo.Language.Lexer;

namespace Ergo.UnitTests;
public class LexerTests
{
    protected Token Expect(Token.Type type, string input)
    {
        var stream = ErgoFileStream.Create(input);
        var lexer = new ErgoLexer(stream, new());
        var result = lexer.ReadNext();
        var token = result.GetOrThrow();
        Assert.Equal(type, token.Type_);
        return token;
    }
    [Theory]
    [InlineData("a")]
    [InlineData("alwkjfneijorgniejrgneirjgnnqwfjnasjhfbashfbd")]
    [InlineData("snake_case")]
    [InlineData("camelCase")]
    public void Term(string input)
    {
        var token = Expect(Token.Type.Term, input);
        Assert.Equal(input, token.Value);
    }
    [Theory]
    [InlineData("'single quotes'")]
    [InlineData("\"double quotes\"")]
    public void String(string input)
    {
        var token = Expect(Token.Type.String, input);
        var expected = input.Replace("'", "").Replace("\"", "");
        Assert.Equal(expected, token.Value);
    }
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("3.168374235")]
    [InlineData("560342354.3534694")]
    public void Number(string input)
    {
        var token = Expect(Token.Type.Number, input);
        var expected = double.Parse(input);
        Assert.InRange(expected - (double)token.Value, -0.001, 0.001);
    }
    [Theory]
    [InlineData("% this is a single line comment")]
    public void Comment(string input)
    {
        var token = Expect(Token.Type.Comment, input);
        var expected = input.TrimStart('%', ' ');
        Assert.Equal(expected, token.Value);
    }
    [Theory]
    [InlineData("%: this is a documentation comment:\n%: it can span multiple lines!")]
    public void Documentation(string input)
    {
        var token = Expect(Token.Type.Documentation, input);
        var expected = input.Replace("%: ", "");
        Assert.Equal(expected, token.Value);
    }
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("!")]
    public void Keyword(string input)
    {
        var token = Expect(Token.Type.Keyword, input);
        var expected = input;
        Assert.Equal(expected, token.Value);
    }
    [Theory]
    [InlineData(",")]
    [InlineData(":-")]
    public void Operator(string input)
    {
        var token = Expect(Token.Type.Operator, input);
        var expected = input;
        Assert.Equal(expected, token.Value);
    }
    [Theory]
    [InlineData(".")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("{")]
    [InlineData("}")]
    public void Punctuation(string input)
    {
        var token = Expect(Token.Type.Punctuation, input);
        var expected = input;
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData(":- 'a' , B , 3 .", 
        Token.Type.Operator, Token.Type.String, Token.Type.Operator, Token.Type.Term, Token.Type.Operator, Token.Type.Number, Token.Type.Punctuation)]
    [InlineData(":- 3.5 , a .",
        Token.Type.Operator, Token.Type.Number, Token.Type.Operator, Token.Type.Term, Token.Type.Punctuation)]
    public void Chain(string input, params Token.Type[] types)
    {
        var inputs = input.Split(' ');
        foreach(var (type, chunk) in types.Zip(inputs))
            Expect(type, chunk);
    }
}