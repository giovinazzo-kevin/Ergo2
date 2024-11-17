using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Language.Lexing;

internal static partial class LexerUtils
{
    static readonly Regex UnescapeRegex = GetUnescapeRegex();
    public static string Unescape(string s)
    {
        var sb = new StringBuilder();
        var mc = UnescapeRegex.Matches(s, 0);

        foreach (Match m in mc)
        {
            if (m.Length == 1)
            {
                sb.Append(m.Value);
            }
            else
            {
                if (m.Value[1] is >= '0' and <= '7')
                {
                    var i = Convert.ToInt32(m.Value[1..], 8);
                    sb.Append((char)i);
                }
                else if (m.Value[1] == 'u')
                {
                    var i = Convert.ToInt32(m.Value[2..], 16);
                    sb.Append((char)i);
                }
                else if (m.Value[1] == 'U')
                {
                    var i = Convert.ToInt32(m.Value[2..], 16);
                    sb.Append(char.ConvertFromUtf32(i));
                }
                else
                {
                    switch (m.Value[1])
                    {
                        case 'a':
                            sb.Append('\a');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'v':
                            sb.Append('\v');
                            break;
                        default:
                            sb.Append(m.Value[1]);
                            break;
                    }
                }
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex("\\\\[abfnrtv?\"'\\\\]|\\\\[0-3]?[0-7]{1,2}|\\\\u[0-9a-fA-F]{4}|\\\\U[0-9a-fA-F]{8}|.", RegexOptions.Compiled)]
    private static partial Regex GetUnescapeRegex();
}
