namespace CodeCrafters.Shell.Commands;

public sealed class CommandExecutor
{
    public void Execute(string command, string[] args)
    {
        if (BuiltinCommands.Execute(command, args, Console.Out))
        {
            return;
        }

        ExecuteFile(command, args);
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
