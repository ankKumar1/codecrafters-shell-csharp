using System.Diagnostics;

namespace CodeCrafters.Shell.Commands;

public static class ExternalProgramRunner
{
    public static void Run(
        string path,
        string commandName,
        string[] args,
        Stream? output = null,
        Stream? error = null)
    {
        ProcessStartInfo startInfo = CreateStartInfo(path, commandName, args);
        startInfo.RedirectStandardOutput = output != null;
        startInfo.RedirectStandardError = error != null;

        try
        {
            using Process? process = Process.Start(startInfo);

            if (process != null)
            {
                CopyRedirectedStreams(process, output, error);
            }

            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running external program: {ex.Message}");
        }
    }

    public static Process? StartBackground(string path, string commandName, string[] args)
    {
        try
        {
            return Process.Start(CreateStartInfo(path, commandName, args));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running external program: {ex.Message}");
            return null;
        }
    }

    private static ProcessStartInfo CreateStartInfo(string path, string commandName, string[] args)
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

        return startInfo;
    }

    private static string EscapeSingleQuotes(string value)
    {
        return value.Replace("'", "'\\''");
    }

    private static void CopyRedirectedStreams(Process process, Stream? output, Stream? error)
    {
        if (output != null && error != null)
        {
            Task.WaitAll(
                Task.Run(() => process.StandardOutput.BaseStream.CopyTo(output)),
                Task.Run(() => process.StandardError.BaseStream.CopyTo(error))
            );
            return;
        }

        if (output != null)
            process.StandardOutput.BaseStream.CopyTo(output);

        if (error != null)
            process.StandardError.BaseStream.CopyTo(error);
    }
}
