namespace Ergo.Lang.Lexing;

public readonly record struct LexerState(int Line, int Column, long StreamPos, string Context)
{
    public static readonly LexerState Start = new(0, 0, 0, new string(' ', 16));
}
