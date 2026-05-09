using CodeCrafters.Shell.Utilities;

namespace CodeCrafters.Shell.Commands;

public static class BuiltinCommands
{
    public static bool IsBuiltin(string command)
    {
        return Utils.Builtins.Contains(command);
    }

    public static bool Execute(string command, string[] args, TextWriter output)
    {
        string argument = string.Join(' ', args);

        switch (command)
        {
            case "echo":
                Echo(argument, output);
                return true;
            case "type":
                Type(argument, output);
                return true;
            case "pwd":
                Pwd(output);
                return true;
            case "cd":
                Cd(argument, output);
                return true;
            case "history":
                History(args, output);
                return true;
            default:
                return false;
        }
    }

    public static void Echo(string args, TextWriter output)
    {
        output.WriteLine(args);
    }

    public static void Type(string args, TextWriter output)
    {
        if (Utils.Builtins.Contains(args))
        {
            output.WriteLine($"{args} is a shell builtin");
            return;
        }

        string? fullPath = FileExecution.FindInPath(args);

        if (!string.IsNullOrEmpty(fullPath))
        {
            output.WriteLine($"{args} is {fullPath}");
            return;
        }

        output.WriteLine($"{args}: not found");
    }

    public static void Pwd(TextWriter output)
    {
        output.WriteLine(Directory.GetCurrentDirectory());
    }

    public static void Cd(string args, TextWriter output)
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
            output.WriteLine($"cd: {args}: No such file or directory");
        }
        catch (Exception ex)
        {
            output.WriteLine($"cd: {ex.Message}");
        }
    }

    public static void History(string[] args, TextWriter output)
    {
        if (args.Length >= 2 && args[0] == "-r")
        {
            ReadHistoryFile(args[1], output);
            return;
        }

        int i = 0;

        if (args.Length > 0 && int.TryParse(args[0], out int count))
        {
            i = Math.Max(0, Utils.history.Count - count);
        }

        while (i < Utils.history.Count)
        {
            output.WriteLine($"{i + 1} {Utils.history[i]}");
            i++;
        }

    }

    private static void ReadHistoryFile(string path, TextWriter output)
    {
        try
        {
            string[] lines = File.ReadAllLines(path);
            foreach (var line in lines)
                Utils.history.Add(line);
        }
        catch (FileNotFoundException)
        {
            output.WriteLine($"history: {path}: No such file or directory");
        }
        catch (DirectoryNotFoundException)
        {
            output.WriteLine($"history: {path}: No such file or directory");
        }
        catch (Exception ex)
        {
            output.WriteLine($"history: {ex.Message}");
        }
    }
}
