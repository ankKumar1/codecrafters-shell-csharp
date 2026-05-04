namespace CodeCrafters.Shell.Commands;

public sealed class CommandExecutor
{
    public void Execute(string command, string[] args)
    {
        string argument = string.Join(' ', args);

        switch (command)
        {
            case "echo":
                BuiltinCommands.Echo(argument, Console.Out);
                break;
            case "type":
                BuiltinCommands.Type(argument, Console.Out);
                break;
            case "pwd":
                BuiltinCommands.Pwd(Console.Out);
                break;
            case "cd":
                BuiltinCommands.Cd(argument, Console.Out);
                break;
            case "history":
                BuiltinCommands.History(Console.Out);
                break;
            default:
                ExecuteFile(command, args);
                break;
        }
    }

    private static void ExecuteFile(string command, string[] args)
    {
        string? fullPath = FileExecution.FindInPath(command);

        if (!string.IsNullOrEmpty(fullPath))
        {
            ExternalProgramRunner.Run(fullPath, command, args);
            return;
        }

        Console.WriteLine($"{command}: command not found");
    }
}
