using CodeCrafters.Shell.Input;
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
            case "jobs":
                Jobs(args, output);
                return true;
            case "complete":
                Complete(args);
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
            CommandHistory.ReadFromFile(args[1], output);
            return;
        }

        if (args.Length == 2 && args[0] == "-w")
        {
            CommandHistory.WriteToFile(args[1], output);
            return;
        }

        if (args.Length == 2 && args[0] == "-a")
        {
            CommandHistory.AppendToFile(args[1], output);
            return;
        }

        int? count = null;

        if (args.Length > 0 && int.TryParse(args[0], out int parsedCount))
            count = parsedCount;

        foreach (var entry in CommandHistory.GetEntries(count))
            output.WriteLine($"{entry.Number} {entry.Command}");

    }

    public static void Jobs(string[] args, TextWriter output)
    {
        BackgroundJobs.ListRunning(output);
    }

    public static void Complete(string[] args)
    {
        Completion.Complete(args);
    }
}
