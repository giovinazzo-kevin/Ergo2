using Ergo.IO;
using Ergo.Language.Ast;
using Ergo.Language.Lexer.WellKnown;
using Ergo.Shared.Types;
using System.Text;

namespace Ergo.Language.Lexer;

public class ErgoLexer : IDisposable
{
    protected readonly ErgoFileStream File;
    protected readonly OperatorLookup Lookup;
    protected bool Eof => File.Eof;
    private LexerState _state = LexerState.Start;
    public LexerState State => _state = _state with { 
        StreamPos = File.Stream.Position
    };
    public ErgoLexer(ErgoFileStream file, OperatorLookup? lookup = null)
    {
        File = file;
        Lookup = lookup ?? new();
        File.Stream.Seek(0, SeekOrigin.Begin);
    }
    public void Seek(LexerState seekState)
    {
        if (State.LP == seekState.LP && State.StreamPos == seekState.StreamPos)
            return;
        if (State.StreamPos != seekState.StreamPos)
            File.Stream.Seek(seekState.StreamPos, SeekOrigin.Begin);
        _state = seekState;
    }
    public Tx<LexerState> Transact() =>
         new(State, Seek);
    public Maybe<Token> PeekNext()
    {
        using var _ = Transact();
        return ReadNext();
    }
    public Maybe<Operator> ReadNextOperator(Func<Operator, bool> match) =>
         ReadNext()
        .Where(x => x.Type_ == Token.Type.Operator)
        .Map(lookahead => Lookup.GetOperatorsFromFunctor((string)lookahead.Value)
            .Select(x => x.Where(match)))
        .Where(x => x.Any())
        .Select(ops => ops.MaxBy(x => x.Precedence));
    public Maybe<Token> ReadNext() =>
         ReadNextImpl();
    protected Maybe<Token> ReadNextImpl()
    {
        SkipWhitespace();
        if (Eof)
            return default;
        var ch = Peek();
        if (IsStringDelimiter(ch))
            return ReadString(ch);
        if (IsNumberStart(ch))
            return ReadNumber();
        if (IsIdentifierStart(ch))
            return ReadIdentifier();
        if (IsOperatorPiece(ch, 0) && ReadOperator().TryGetValue(out var op))
            return op;
        if (IsPunctuationPiece(ch))
            return ReadPunctuation();
        if (IsDocumentationCommentStart(ch))
            return ReadDocumentationComment();
        if (IsSingleLineCommentStart(ch))
            return ReadSingleLineComment();
        return default;
    }
    #region Helpers
    public char Peek()
    {
        using var tx = Transact();
        if (Eof)
            return '\0';
        return File.ReadUTF8Char();
    }
    public char PeekAhead(int i)
    {
        using var tx = Transact();
        if (State.StreamPos + i >= File.Stream.Length)
            return '\0';
        File.Stream.Seek(i, SeekOrigin.Current);
        return File.ReadUTF8Char();
    }
    public char Read()
    {
        var c = File.ReadUTF8Char();
        if (IsCarriageReturn(c))
            _state = _state with { Column = 0 };
        else if (IsNewline(c))
            _state = _state with { Column = 0, Line = State.Line + 1 };
        else
            _state = _state with { Column = _state.Column + 1 };
        _state = _state with { Context = _state.Context.Remove(0, 1) + c };
        return c;
    }
    public void SkipWhitespace()
    {
        while (!Eof && char.IsWhiteSpace(Peek()))
            Read();
    }
    public void SkipComments()
    {
        while (!Eof && IsSingleLineCommentStart(Peek()))
        {
            using var tx = Transact();
            _ = Read();
            var c = Read();
            tx.Dispose();
            if (IsDocumentationCommentStart(c))
                break;
            ReadSingleLineComment();
            SkipWhitespace();
        }
    }
    #endregion

    #region Tokens
    protected Token ReadString(char delim)
    {
        var sb = new StringBuilder();
        var escapeSb = new StringBuilder();
        Read(); // Skip opening quotes
        while (!Eof)
        {
            var escaping = false;
            if (Peek() == '\\')
            {
                escaping = true;
                escapeSb.Append('\\');
                Read();
            }
            if (Eof)
                break;
            if (Peek() != delim || escaping)
            {
                escapeSb.Append(Read());
                sb.Append(LexerUtils.Unescape(escapeSb.ToString()));
                escapeSb.Clear();
            }
            else
            {
                Read();
                break;
            }
        }
        return Token.FromString(sb.ToString());
    }
    protected Token ReadNumber()
    {
        var (number, integralPlaces, sign) = (0d, -1, true);
        var c = Peek();
        if (IsSign(c))
            sign = Read() == '+';
        for (var i = 0; IsNumberPiece(c = Peek()); ++i)
        {
            if (IsDigit(c))
            {
                var digit = int.Parse(Read().ToString());
                if (integralPlaces == -1)
                    number = number * 10 + digit;
                else
                    number += digit / Math.Pow(10, i - integralPlaces);
            }
            else if (IsDecimalDelimiter(c))
            {
                if (integralPlaces != -1) break;
                using var tx = Transact();
                Read();
                if (Eof || !IsNumberPiece(Peek()))
                    break;
                tx.Commit();
                integralPlaces = i;
            }
        }
        return Token.FromNumber(number * (sign ? 1 : -1));
    }
    protected Token ReadIdentifier()
    {
        var sb = new StringBuilder();
        while (IsIdentifierPiece(Peek()))
            sb.Append(Read());
        var str = sb.ToString();
        if (IsKeyword(str))
            return Token.FromKeyword(str);
        return Token.FromTerm(str);
    }
    protected Token ReadSingleLineComment()
    {
        var sb = new StringBuilder();
        Read(); // Discard %
        SkipWhitespace();
        while (!Eof && !IsNewline(Peek()))
            sb.Append(Read());
        return Token.FromComment(sb.ToString().Trim());
    }
    protected Token ReadDocumentationComment()
    {
        var c = '\0';
        var sb = new StringBuilder();
        var lines = new List<string>();
        while (!Eof && IsDocumentationCommentStart(c = Peek()))
        {
            sb.Clear();
            Read(); // Discard %
            Read(); // Discard :
            SkipWhitespace();
            while (!Eof && !IsNewline(c))
                sb.Append(c = Read());
            lines.Add(sb.ToString().Trim());
            SkipWhitespace();
        }
        return Token.FromDocumentationComment(string.Join('\n', lines));
    }
    protected Token ReadPunctuation()
    {
        using var tx = Transact();
        var (set, i) = (Symbols.Punctuation.ToList(), 0);
        while (IsPunctuationPiece(Peek()))
        {
            var ch = Read();
            for (var o = set.Count - 1; o >= 0; o--)
            {
                if (set[o].Length <= i || set[o][i] != ch)
                    set.RemoveAt(o);
            }
            if (set.Count >= 1)
            {
                i++;
                if (set.Count == 1)
                {
                    while (!Eof && i++ < set[0].Length) 
                        Read();
                    break;
                }
            }
            else
                throw new LexerException(LexerError.UnrecognizedPunctuation, State);
        }
        tx.Commit();
        SkipWhitespace();
        return Token.FromPunctuation(set.OrderBy(s => s.Length).First());
    }
    protected Maybe<Token> ReadOperator()
    {
        using var tx = Transact();
        List<string> set = [.. Lookup.Functors];
        var (peek, i) = (Peek(), 0);
        while (IsOperatorPiece(peek, i) && set.SelectMany(x => x).Contains(peek))
        {
            var ch = Read();
            for (var o = set.Count - 1; o >= 0; o--)
                if (set[o].Length <= i || set[o][i] != ch)
                    set.RemoveAt(o);
            if (set.Count < 1)
                return default;
            i++;
            if (set.Count == 1)
            {
                while (i++ < set[0].Length)
                {
                    if (Eof || Read() != set[0][i - 1])
                        return default;
                }
                break;
            }
            peek = Peek();
        }
        tx.Commit();
        var op = set.First();
        var token = Token.FromOperator(op);
        return token;
    }
    #endregion

    #region Predicates
    protected bool IsSingleLineCommentStart(char c) => 
         Symbols.Comment
        .Any(StartsWith);
    protected bool IsDocumentationCommentStart(char c) => 
         Symbols.DocComment
        .Any(StartsWith);
    protected static bool IsStringDelimiter(char c) => 
         Symbols.StringDelimiter
        .Contains(c);
    protected static bool IsKeyword(string s) => 
         Symbols.Keyword
        .Contains(s);
    protected static bool IsPunctuationPiece(char c) => 
         Symbols.Punctuation
        .SelectMany(p => p).Contains(c);
    protected static bool IsIdentifierStart(char c) =>
        char.IsLetter(c)
        || Symbols.IdentifierStart
          .Contains(c);
    protected static bool IsIdentifierPiece(char c) => 
        IsIdentifierStart(c) 
        || IsDigit(c);
    protected static bool IsCarriageReturn(char c) => 
        c == '\r';
    protected static bool IsNewline(char c) => 
        c == '\n';
    protected static bool IsDigit(char c) => 
        char.IsDigit(c);
    protected bool IsSign(char c, int p = 0) => 
        (c == '-' || c == '+') 
        && PeekAhead(p + 1) is var d 
        && IsNumberPiece(d, p + 1);
    protected bool IsNumberStart(char c) => 
        IsSign(c) 
        || IsNumberPiece(c);
    protected bool IsNumberPiece(char c, int p = 0) => 
        IsDecimalDelimiter(c, p) 
        || IsDigit(c);
    protected bool IsDecimalDelimiter(char c, int p = 0) => 
        c == '.' 
        && PeekAhead(p + 1) is var d 
        && IsDigit(d);
    protected bool IsOperatorPiece(char c, int index)
    {
        if (c == '\\') 
            return true;
        if (!(PeekAhead(1) is var next))
            return false;
        var symbols = Lookup.GetNthSymbols(index)
            .ToHashSet();
        if (symbols.Contains(c))
        {
            // Disambiguate between . as an operator for dict dereferencing, and . as a clause terminator or decimal separator
            if (c == '.' && index == 0)
            {
                if (IsPunctuationPiece(next))
                    return false;
                if (char.IsWhiteSpace(next) || IsSingleLineCommentStart(next) || IsDocumentationCommentStart(next))
                    // - End of clause, which means that this is not an operator
                    return false;
                return true;
            }
            return true;
        }
        return false;
    }
    bool StartsWith(string x)
    {
        for (var i = 0; i < x.Length; ++i)
            if (PeekAhead(i) != x[i])
                return false;
        return true;
    }
    #endregion

    public void Dispose()
    {
        File.Stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
