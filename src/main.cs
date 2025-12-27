using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static readonly string[] Builtins = ["echo", "type", "exit", "pwd", "cd"];
    const int X_OK = 1;
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine() ?? "";

            if (command == "exit")
            {
                break;
            }

            if (command != null)
            {
                ExecuteCommand(command);
            }
        }

    }

    public static void ExecuteCommand(string command)
    {
        int idx = command.IndexOf(' ');

        string cmd = idx == -1 ? command : command.Substring(0, idx);
        string args = idx == -1 ? string.Empty : command.Substring(idx + 1).Trim();

        if (command == "echo")
        {
            EchoCommand(args);
        }
        else if (command == "type")
        {
            TypeCommand(args);
        }
        else if (command == "pwd")
        {
            PwdCommand();
        }
        else if (command == "cd")
        {
            CdCommand(args);
        }
        else
        {
            ExecuteFiles(command);
        }
    }

    static void EchoCommand(string args)
    {
        var argList = HandleQuotes(args);
        Console.WriteLine(string.Join(' ', argList));
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

        string fullPath = GetFullPath(args);
        if (!string.IsNullOrEmpty(fullPath))
        {
            Console.WriteLine($"{args} is {fullPath}");
            return;
        }

        Console.WriteLine($"{args}: not found");
    }

    static void ExecuteFiles(string input)
    {
        var tokens = HandleQuotes(input);

        if (tokens.Count == 0)
            return;

        string executable = tokens[0];
        string[] arguments = tokens.ToArray();

        string? fullPath = GetFullPath(executable);
        if (!string.IsNullOrEmpty(fullPath))
        {
            RunExternalProgram(fullPath, executable,
                               arguments);

            return;
        }
        Console.WriteLine($"{executable}: command not found");
    }

    static void RunExternalProgram(string path, string commandName,
                                string[] args)
    {
        // To properly set argv[0] to just the command name (not full path),
        // we need to use exec -a through a shell
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "/bin/sh";

        // Build the command: exec -a <commandName> <fullPath> <args...>
        // exec -a allows us to set argv[0] explicitly
        var escapedArgs = args.Select(a => $"'{a.Replace("'", "'\\''")}'");
        var commandLine =
            $"exec -a '{commandName}' '{path}' {string.Join(" ", escapedArgs)}";

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(commandLine);
        startInfo.UseShellExecute = false;

        try
        {
            // Start the process and wait for it to complete
            using (Process process = Process.Start(startInfo))
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

    static string GetFullPath(string command)
    {
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (string dir in pathEnv.Split(':'))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;
                string fullPath = Path.Combine(dir, command);
                if (File.Exists(fullPath) && IsExecutable(fullPath))
                {
                    return fullPath;
                }
            }
        }
        return string.Empty;
    }

    static bool IsExecutable(string path)
    {
        return access(path, X_OK) == 0;
    }

    [DllImport("libc", SetLastError = true)]
    static extern int access(string pathname, int mode);
}
