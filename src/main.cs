using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static readonly string[] Builtins = ["echo", "type", "exit"];
    const int X_OK = 1;
    static void Main()
    {
        // TODO: Uncomment the code below to pass the first stage
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine();

            if (command == "exit")
            {
                break;
            }

            if (command != null)
            {
                int idx = command.IndexOf(' ');

                string cmd = idx == -1 ? command : command.Substring(0, idx);
                string args = idx == -1 ? string.Empty : command.Substring(idx + 1).Trim();

                ExecuteCommand(cmd, args);
            }
        }

    }

    public static void ExecuteCommand(string command, string args)
    {
        if (command == "echo")
        {
            Console.WriteLine(args);
        }
        else if (command == "type")
        {
            TypeCommand(args);
        }
        else
        {
            ExecuteFiles(command, args);
        }
    }

    public static void TypeCommand(string args)
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

    static void ExecuteFiles(string command, string args)
    {
        string fullPath = GetFullPath(command);
        if (!string.IsNullOrEmpty(fullPath))
        {
            RunExternalProgram(fullPath, command,
                               args.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return;
        }
        Console.WriteLine($"{command} {args} : command not found");
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
                process.WaitForExit();
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
