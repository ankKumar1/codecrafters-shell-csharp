using CodeCrafters.Shell.Utilities;

namespace CodeCrafters.Shell.Commands;

public static class BuiltinCommands
{
    public static bool IsBuiltin(string command)
    {
        return Utils.Builtins.Contains(command);
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

    public static void History(string args,TextWriter output)
    {
        int  i = 0;
        if(!string.IsNullOrEmpty(args))
        {
            i = Utils.history.Count - int.Parse(args);
        }

        while (i < Utils.history.Count)
        {
            output.WriteLine($"{i + 1} {Utils.history[i]}");
            i++;
        }

    }
}
