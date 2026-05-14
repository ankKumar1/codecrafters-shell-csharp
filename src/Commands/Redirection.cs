namespace CodeCrafters.Shell.Commands;

public static class Redirection
{
    private const string StdoutOperator = ">";
    private const string AppendStdoutOperator = ">>";
    private const string StderrOperator = "2>";

    public static bool TryExecute(List<string> parts)
    {
        int redirectIndex = FindRedirectIndex(parts);

        if (redirectIndex == -1)
            return false;

        if (redirectIndex == 0 || redirectIndex == parts.Count - 1)
        {
            Console.WriteLine("Invalid redirection");
            return true;
        }

        if (HasMultipleRedirects(parts))
        {
            Console.WriteLine("Invalid redirection");
            return true;
        }

        var commandParts = parts.Take(redirectIndex).ToList();
        string outputPath = parts[redirectIndex + 1];
        bool redirectError = parts[redirectIndex] == StderrOperator;
        bool appendOutput = parts[redirectIndex] == AppendStdoutOperator;

        Execute(commandParts, outputPath, redirectError, appendOutput);
        return true;
    }

    private static int FindRedirectIndex(List<string> parts)
    {
        var redirectIndexes = parts
            .Select((part, index) => IsRedirectOperator(part) ? index : -1)
            .Where(index => index != -1)
            .ToList();

        return redirectIndexes.Count == 0 ? -1 : redirectIndexes.Min();
    }

    private static bool HasMultipleRedirects(List<string> parts)
    {
        return parts.Count(IsRedirectOperator) > 1;
    }

    private static bool IsRedirectOperator(string part)
    {
        return part == StdoutOperator ||
               part == AppendStdoutOperator ||
               part == StderrOperator;
    }

    private static void Execute(
        List<string> commandParts,
        string outputPath,
        bool redirectError,
        bool appendOutput)
    {
        string command = commandParts[0];
        string[] args = commandParts.Skip(1).ToArray();

        try
        {
            FileMode fileMode = appendOutput ? FileMode.Append : FileMode.Create;
            using var fileStream = new FileStream(outputPath, fileMode, FileAccess.Write);

            if (redirectError)
            {
                ExecuteWithStderrRedirect(command, args, fileStream);
                return;
            }

            ExecuteWithStdoutRedirect(command, args, fileStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"redirection: {ex.Message}");
        }
    }

    private static void ExecuteWithStdoutRedirect(string command, string[] args, Stream output)
    {
        using var writer = new StreamWriter(output, leaveOpen: true);

        if (BuiltinCommands.Execute(command, args, writer))
        {
            writer.Flush();
            return;
        }

        writer.Flush();
        RunExternalCommand(command, args, output, null, Console.Error);
    }

    private static void ExecuteWithStderrRedirect(string command, string[] args, Stream error)
    {
        using var errorWriter = new StreamWriter(error, leaveOpen: true);

        if (BuiltinCommands.Execute(command, args, Console.Out))
        {
            errorWriter.Flush();
            return;
        }

        errorWriter.Flush();
        RunExternalCommand(command, args, null, error, errorWriter);
        errorWriter.Flush();
    }

    private static void RunExternalCommand(
        string command,
        string[] args,
        Stream? output,
        Stream? error,
        TextWriter errorWriter)
    {
        string? fullPath = FileExecution.FindInPath(command);

        if (string.IsNullOrEmpty(fullPath))
        {
            errorWriter.WriteLine($"{command}: command not found");
            errorWriter.Flush();
            return;
        }

        ExternalProgramRunner.Run(fullPath, command, args, output, error);
    }
}
