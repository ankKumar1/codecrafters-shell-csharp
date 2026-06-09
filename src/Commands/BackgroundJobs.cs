using System.Diagnostics;

namespace CodeCrafters.Shell.Commands;

public static class BackgroundJobs
{
    private static readonly List<BackgroundJob> Jobs = [];
    private static int _nextJobNumber = 1;

    public static bool TryStart(List<string> parts)
    {
        if (parts.Count == 0 || parts[^1] != "&")
            return false;

        string commandText = string.Join(' ', parts);
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
        Jobs.Add(new BackgroundJob(jobNumber, process, commandText));

        Console.WriteLine($"[{jobNumber}] {process.Id}");
        return true;
    }

    public static void ListRunning(TextWriter output)
    {
        RemoveCompletedJobs();

        var runningJobs = Jobs
            .Where(job => job.IsRunning)
            .OrderBy(job => job.Number)
            .ToList();

        if (runningJobs.Count == 0)
            return;

        int currentJobNumber = runningJobs[^1].Number;
        int? previousJobNumber = runningJobs.Count >= 2
            ? runningJobs[^2].Number
            : null;

        foreach (var job in runningJobs)
        {
            char marker = GetJobMarker(job.Number, currentJobNumber, previousJobNumber);
            output.WriteLine($"[{job.Number}]{marker}  {job.Status,-24}{job.Command}");
        }
    }

    private static char GetJobMarker(int jobNumber, int currentJobNumber, int? previousJobNumber)
    {
        if (jobNumber == currentJobNumber)
            return '+';

        if (jobNumber == previousJobNumber)
            return '-';

        return ' ';
    }

    private static void RemoveCompletedJobs()
    {
        Jobs.RemoveAll(job => !job.IsRunning);
    }
}
