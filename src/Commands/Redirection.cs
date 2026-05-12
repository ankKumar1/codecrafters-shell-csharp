namespace CodeCrafters.Shell.Commands;

public static class Redirection
{
    public static bool TryExecute(List<string> parts)
    {
        int redirectIndex = parts.IndexOf(">");

        if (redirectIndex == -1)
            return false;

        if (redirectIndex == 0 || redirectIndex == parts.Count - 1)
        {
            Console.WriteLine("Invalid redirection");
            return true;
        }

        if (parts.LastIndexOf(">") != redirectIndex)
        {
            Console.WriteLine("Invalid redirection");
            return true;
        }

        var commandParts = parts.Take(redirectIndex).ToList();
        string outputPath = parts[redirectIndex + 1];

        Execute(commandParts, outputPath);
        return true;
    }

    private static void Execute(List<string> commandParts, string outputPath)
    {
        string command = commandParts[0];
        string[] args = commandParts.Skip(1).ToArray();

        try
        {
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(outputStream, leaveOpen: true);

            if (BuiltinCommands.Execute(command, args, writer))
            {
                writer.Flush();
                return;
            }

            writer.Flush();
            RunExternalCommand(command, args, outputStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"redirection: {ex.Message}");
        }
    }

    private static void RunExternalCommand(string command, string[] args, Stream output)
    {
        string? fullPath = FileExecution.FindInPath(command);

        if (string.IsNullOrEmpty(fullPath))
        {
            Console.WriteLine($"{command}: command not found");
            return;
        }

        ExternalProgramRunner.Run(fullPath, command, args, output);
    }
}
