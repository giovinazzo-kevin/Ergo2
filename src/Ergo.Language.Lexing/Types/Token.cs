namespace Ergo.Language.Lexing;

public readonly record struct Token(Token.Type Type_, object Value)
{
    public enum Type
    {
        String
        , Integer
        , Decimal
        , Keyword
        , Term
        , Punctuation
        , Operator
        , Comment
        , Documentation
    }

    public static Token FromString(string value) => new(Type.String, value);
    public static Token FromNumber(int value) => new(Type.Integer, value);
    public static Token FromNumber(double value) => new(Type.Decimal, value);
    public static Token FromKeyword(string value) => new(Type.Keyword, value);
    public static Token FromTerm(string value) => new(Type.Term, value);
    public static Token FromPunctuation(string value) => new(Type.Punctuation, value);
    public static Token FromOperator(string value) => new(Type.Operator, value);
    public static Token FromComment(string value) => new(Type.Comment, value);
    public static Token FromDocumentationComment(string value) => new(Type.Documentation, value);
}
