using System.Text;

namespace CodeCrafters.Shell.Parsing;

public static class CommandParser
{
    public static List<string> Parse(string input)
    {
        var args = new List<string>();
        var current = new StringBuilder();

        bool inSingleQuote = false;
        bool inDoubleQuote = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '\\')
            {
                if (!inSingleQuote && !inDoubleQuote)
                {
                    if (i + 1 < input.Length)
                    {
                        current.Append(input[i + 1]);
                        i++;
                    }

                    continue;
                }

                if (inDoubleQuote)
                {
                    if (i + 1 < input.Length &&
                        (input[i + 1] == '"' || input[i + 1] == '\\'))
                    {
                        current.Append(input[i + 1]);
                        i++;
                    }
                    else
                    {
                        current.Append('\\');
                    }

                    continue;
                }

                current.Append('\\');
                continue;
            }

            if (c == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (c == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                continue;
            }

            if ((c == '|' || c == '>') && !inSingleQuote && !inDoubleQuote)
            {
                if (current.Length > 0)
                {
                    if (c != '>' || current.ToString() != "1")
                        args.Add(current.ToString());

                    current.Clear();
                }

                args.Add(c.ToString());
                continue;
            }

            if (char.IsWhiteSpace(c) && !inSingleQuote && !inDoubleQuote)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            args.Add(current.ToString());

        return args;
    }
}
