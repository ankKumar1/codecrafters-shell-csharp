using System.Diagnostics;
using System.Text;

class Program
{
    static readonly string[] Builtins = ["echo", "type", "exit", "pwd", "cd"];
    static readonly string[] AutoCompleteBuiltins = ["echo", "exit"];

    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            string input = ReadLineWithTabCompletion();

            if (input == "exit")
            {
                break;
            }

            var parts = HandleQuotes(input);

            if (parts.Count == 0)
                continue;

            var command = parts[0];
            var args = parts.Skip(1)
                           .ToArray();
            ExecuteCommand(command, args);
        }

    }

    static string ReadLineWithTabCompletion()
    {
        var buffer = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            // ENTER
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            // BACKSPACE
            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                    Console.Write("\b \b");
                }
                continue;
            }

            // TAB → autocomplete
            if (key.Key == ConsoleKey.Tab)
            {
                Autocomplete(buffer);
                continue;
            }

            // Normal character
            Console.Write(key.KeyChar);
            buffer.Append(key.KeyChar);
        }

        return buffer.ToString();
    }

    static void Autocomplete(StringBuilder buffer)
    {
        string text = buffer.ToString();

        // Only autocomplete the first word
        if (text.Contains(' '))
            return;

        var builtinMatch = AutoCompleteBuiltins
            .FirstOrDefault(b => b.StartsWith(text, StringComparison.Ordinal));

        if (builtinMatch != null)
        {
            ReplaceBuffer(buffer, builtinMatch);
            return;
        }

        // 2️⃣ Try executable completion from PATH
        var executables = FindExecutablesStartingWith(text);

        if (executables.Count == 1)
        {
            ReplaceBuffer(buffer, executables[0]);
            return;
        }

        // 3️⃣ No match → bell
        Console.Write("\x07");
    }

    static void ReplaceBuffer(StringBuilder buffer, string completion)
    {
        // Erase current text
        for (int i = 0; i < buffer.Length; i++)
            Console.Write("\b \b");

        buffer.Clear();

        buffer.Append(completion);
        buffer.Append(' ');
        Console.Write(completion + " ");
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

        string? fullPath = FindInPath(args);
        if (!string.IsNullOrEmpty(fullPath))
        {
            Console.WriteLine($"{args} is {fullPath}");
            return;
        }

        Console.WriteLine($"{args}: not found");
    }

    static void ExecuteFiles(string command, string[] args)
    {
        string? fullPath = FindInPath(command);
        if (!string.IsNullOrEmpty(fullPath))
        {
            RunExternalProgram(fullPath, command, args);

            return;
        }
        Console.WriteLine($"{command}: command not found");
    }

    static List<string> FindExecutablesStartingWith(string prefix)
    {
        var results = new HashSet<string>();
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        if (pathEnv == null)
            return results.ToList();

        var directories = pathEnv.Split(Path.PathSeparator);

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
                continue;

            try
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    var name = Path.GetFileName(file);

                    if (!name.StartsWith(prefix, StringComparison.Ordinal))
                        continue;

                    if (IsExecutable(file))
                        results.Add(name);
                }
            }
            catch
            {
                // Ignore unreadable directories
            }
        }

        return results.ToList();
    }


    public static string? FindInPath(string command)
    {
        // Get the PATH environment variable
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        // Return null if PATH is not set
        if (pathEnv == null)
            return null;

        // Split PATH into individual directories
        var directories = pathEnv.Split(Path.PathSeparator);

        // Search each directory for the command
        foreach (var dir in directories)
        {
            // Construct the full path to the potential executable
            var fullPath = Path.Combine(dir, command);

            // Check if file exists and is executable
            if (File.Exists(fullPath) && IsExecutable(fullPath))
            {
                return fullPath;
            }
        }

        // Command not found in any PATH directory
        return null;
    }

    // Check if a file has execute permissions (Unix-style)
    private static bool IsExecutable(string path)
    {
        try
        {
            // Get Unix file permissions (only available on Unix-like systems)
            var unixFileMode = File.GetUnixFileMode(path);
            // Check if any execute permission is set (user, group, or other)
            return (unixFileMode & UnixFileMode.UserExecute) != 0 ||
                   (unixFileMode & UnixFileMode.GroupExecute) != 0 ||
                   (unixFileMode & UnixFileMode.OtherExecute) != 0;
        }
        catch
        {
            // If unable to check permissions, assume not executable
            return false;
        }
    }

    // Run an external program with specified arguments
    public static void RunExternalProgram(string path, string commandName,
                                          string[] args)
    {
        // To properly set argv[0] to just the command name (not full path),
        // we need to use exec -a through a shell
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "/bin/sh";

        // Build the command: exec -a <commandName> <fullPath> <args...>
        // exec -a allows us to set argv[0] explicitly
        // Escape single quotes in commandName and path
        var escapedCommandName = commandName.Replace("'", "'\\''");
        var escapedPath = path.Replace("'", "'\\''");
        var escapedArgs = args.Select(a => $"'{a.Replace("'", "'\\''")}'");
        var commandLine =
            $"exec -a '{escapedCommandName}' '{escapedPath}' {string.Join(" ", escapedArgs)}";

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(commandLine);
        startInfo.UseShellExecute = false;

        try
        {
            // Start the process and wait for it to complete
            using (Process? process = Process.Start(startInfo))
            {
                // Wait for the external program to finish execution
                process?.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            // Display error message if program execution fails
            Console.WriteLine($"Error running external program: {ex.Message}");
        }
    }
}
