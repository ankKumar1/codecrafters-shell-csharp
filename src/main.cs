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

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (string dir in pathEnv.Split(':'))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;

                string fullPath = Path.Combine(dir, args);

                if (File.Exists(fullPath))
                {
                    if (IsExecutable(fullPath))
                    {
                        Console.WriteLine($"{args} is {fullPath}");
                        return;
                    }
                }
            }
        }

        Console.WriteLine($"{args}: not found");
    }

    public static void ExecuteFiles(string command, string args)
    {
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (string dir in pathEnv.Split(':'))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;

                string fullPath = Path.Combine(dir, command);
                if (File.Exists(fullPath))
                {
                    if (IsExecutable(fullPath))
                    {
                        try
                        {
                            var process = new System.Diagnostics.Process();
                            process.StartInfo.FileName = fullPath;
                            process.StartInfo.Arguments = args;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            process.Start();
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            if (!string.IsNullOrEmpty(output))
                                Console.Write(output);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{command}: {e.Message}");
                        }
                        return;
                    }
                }
            }
        }
        Console.WriteLine($"{command} {args} : command not found");
    }

    static bool IsExecutable(string path)
    {
        return access(path, X_OK) == 0;
    }

    [DllImport("libc", SetLastError = true)]
    static extern int access(string pathname, int mode);
}
