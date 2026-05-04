using System.Text;

namespace PersonalFinanceCli.Presentation.Parsing;

public static class Tokenizer
{
    public static IReadOnlyList<string> Tokenize(string commandLine)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return result;
        }

        var sb = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in commandLine)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (sb.Length > 0)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(ch);
            }
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result;
    }
}
