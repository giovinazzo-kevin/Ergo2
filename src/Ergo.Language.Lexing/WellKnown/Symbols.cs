namespace Ergo.Lang.Lexing.WellKnown;

public static class Symbols
{
    public static readonly HashSet<string> True = [
        "true", "⊤"
    ];
    public static readonly HashSet<string> False = [
        "false", "⊥"
    ];
    public static readonly HashSet<string> Cut = [
        "!"
    ];
    public static readonly HashSet<string> Comment = [
        "%"
    ];
    public static readonly HashSet<string> DocComment = [
        "%:"
    ];
    public static readonly HashSet<char> StringDelimiter = [
        '\'', '"'
    ];
    public static readonly HashSet<string> Boolean =
        [.. False, .. True];
    public static readonly HashSet<string> Keyword =
        [.. Cut, .. Boolean];
    public static readonly HashSet<string> Punctuation = [
        "(", ")", "[", "]", "{", "}", "."
    ];
    public static readonly HashSet<char> IdentifierStart = [
        '_', '!', '⊤', '⊥'
    ];
}
