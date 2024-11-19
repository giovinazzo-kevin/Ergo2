using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Shared.Extensions;

public static class StringExtensions
{
    private const char SQ = '\'';
    private const char DQ = '"';
    private static readonly Regex ESCAPE_SQ = new(@"(?<!\\)'", RegexOptions.Compiled);
    private static readonly HashSet<char> NONQUOTED = 
        ['(', ')', '[', ']', '{', '}', '_', '+', '-', '*', '/', ';', '!', ':', '⊤', '⊥'];
    public static string Parenthesized(this string str, bool parens, string openingDelim = "(", string closingDelim = ")") 
        => parens ? $"{openingDelim}{str}{closingDelim}" : str;
    public static string AddQuotesIfNecessary(this string str)
    {
        if (str.Length == 0)
            return str;
        var hasWhitespace = str.Any(char.IsWhiteSpace);
        var hasPunctuation = str.Any(x => (char.IsPunctuation(x) 
            || char.IsSymbol(x)) && !NONQUOTED.Contains(x));
        var looksLikeVariable = char.IsUpper(str[0])
            || str[0] == '_';
        if (!looksLikeVariable && !hasWhitespace && !hasPunctuation)
            return str;
        var escaped = (str.Contains(SQ), str.Contains(DQ)) switch
        {
            (false, _) => SQ + str + SQ,
            (true, false) => DQ + str + DQ,
            (true, true) => SQ + ESCAPE_SQ.Replace(str, SQ.ToString()) + SQ
        };
        return escaped;
    }
    public static string ToCSharpCase(this string s)
    {
        // Assume ergo_case
        var sb = new StringBuilder();
        bool nextCharIsUpper = true;
        for (var i = 0; i < s.Length; ++i)
        {
            if (s[i] == '_')
            {
                nextCharIsUpper = true; // Next character should be in upper case.
            }
            else
            {
                sb.Append(nextCharIsUpper ? char.ToUpper(s[i]) : s[i]);
                nextCharIsUpper = false;
            }
        }

        return sb.ToString();
    }

    public static string ToErgoCase(this string s)
    {
        // Assume PascalCase
        var wasUpper = true;
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; ++i)
        {
            var isUpper = char.IsUpper(s[i]);
            if (i > 0 && !wasUpper && isUpper && s[i - 1] != '_')
            {
                sb.Append('_');
            }

            sb.Append(char.ToLower(s[i]));
            wasUpper = isUpper;
        }

        return sb.ToString();
    }
}