using System.Diagnostics;

namespace CodeCrafters.Shell.Commands;

public static class ExternalProgramRunner
{
    public static void Run(string path, string commandName, string[] args)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "/bin/sh",
            UseShellExecute = false
        };

        var escapedCommandName = EscapeSingleQuotes(commandName);
        var escapedPath = EscapeSingleQuotes(path);
        var escapedArgs = args.Select(arg => $"'{EscapeSingleQuotes(arg)}'");
        var commandLine =
            $"exec -a '{escapedCommandName}' '{escapedPath}' {string.Join(" ", escapedArgs)}";

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(commandLine);

        try
        {
            using Process? process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running external program: {ex.Message}");
        }
    }

    private static string EscapeSingleQuotes(string value)
    {
        return value.Replace("'", "'\\''");
    }
}
