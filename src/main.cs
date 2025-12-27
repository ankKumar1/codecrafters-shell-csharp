class Program
{
    static readonly string[] Builtins = ["echo", "type", "exit", "pwd", "cd"];
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            string input = Console.ReadLine() ?? "";

            if (input == "exit")
            {
                break;
            }

            var parts = HandleQuotes(input);

            if (parts.Count == 0)
                return;

            var command = parts[0];
            var args = parts.Skip(1)
                           .ToArray();
            ExecuteCommand(command, args);
        }

    }

    public static void ExecuteCommand(string command, string[] args)
    {
        string agrument = string.Join(' ', args);

        if (command == "echo")
        {
            EchoCommand(agrument);
        }
        else if (command == "type")
        {
            TypeCommand(agrument);
        }
        else if (command == "pwd")
        {
            PwdCommand();
        }
        else if (command == "cd")
        {
            CdCommand(agrument);
        }
        else
        {
            ExecuteFiles(command, args);
        }
    }

    static void EchoCommand(string args)
    {
        Console.WriteLine(args);
        return;
    }

    static List<string> HandleQuotes(string input)
    {
        var args = new List<string>();
        var current = new System.Text.StringBuilder();

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

    static void PwdCommand()
    {
        Console.WriteLine(Directory.GetCurrentDirectory());
    }

    static void CdCommand(string args)
    {
        try
        {
            if (string.IsNullOrEmpty(args) || args == "~")
            {
                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Directory.SetCurrentDirectory(homePath);
            }
            else
            {
                Directory.SetCurrentDirectory(args);
            }
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"cd: {args}: No such file or directory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"cd: {ex.Message}");
        }
    }


    static void TypeCommand(string args)
    {

        if (Builtins.Contains(args))
        {
            Console.WriteLine($"{args} is a shell builtin");
            return;
        }

        string? fullPath = ProcessRunner.FindInPath(args);
        if (!string.IsNullOrEmpty(fullPath))
        {
            Console.WriteLine($"{args} is {fullPath}");
            return;
        }

        Console.WriteLine($"{args}: not found");
    }

    static void ExecuteFiles(string command, string[] args)
    {
        string? fullPath = ProcessRunner.FindInPath(command);
        if (!string.IsNullOrEmpty(fullPath))
        {
            ProcessRunner.RunExternalProgram(fullPath, command, args);

            return;
        }
        Console.WriteLine($"{command}: command not found");
    }
}
