using System.Diagnostics;

namespace CodeCrafters.Shell.Commands;

public static class BackgroundJobs
{
    private static readonly List<Job> Jobs = [];
    private static int _nextJobNumber = 1;

    public static bool TryStart(List<string> parts)
    {
        if (parts.Count == 0 || parts[^1] != "&")
            return false;

        parts.RemoveAt(parts.Count - 1);

        if (parts.Count == 0)
            return true;

        string command = parts[0];
        string[] args = parts.Skip(1).ToArray();
        string? fullPath = FileExecution.FindInPath(command);

        if (string.IsNullOrEmpty(fullPath))
        {
            Console.WriteLine($"{command}: command not found");
            return true;
        }

        Process? process = ExternalProgramRunner.StartBackground(fullPath, command, args);

        if (process == null)
            return true;

        int jobNumber = _nextJobNumber++;
        Jobs.Add(new Job(jobNumber, process));

        Console.WriteLine($"[{jobNumber}] {process.Id}");
        return true;
    }

    private sealed record Job(int Number, Process Process);
}
