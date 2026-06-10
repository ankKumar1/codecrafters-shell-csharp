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

        parts.RemoveAt(parts.Count - 1);

        if (parts.Count == 0)
            return true;

        string commandText = string.Join(' ', parts);
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
        var jobsToDisplay = Jobs
            .OrderBy(job => job.Number)
            .ToList();

        if (jobsToDisplay.Count == 0)
            return;

        int? currentJobNumber = jobsToDisplay.Count > 0
            ? jobsToDisplay[^1].Number
            : null;
        int? previousJobNumber = jobsToDisplay.Count >= 2
            ? jobsToDisplay[^2].Number
            : null;

        foreach (var job in jobsToDisplay)
        {
            char marker = GetJobMarker(job.Number, currentJobNumber, previousJobNumber);
            output.WriteLine($"[{job.Number}]{marker}  {FormatStatus(job.Status)}{job.Command}");
        }

        RemoveCompletedJobs();
    }

    private static char GetJobMarker(int jobNumber, int? currentJobNumber, int? previousJobNumber)
    {
        if (jobNumber == currentJobNumber)
            return '+';

        if (jobNumber == previousJobNumber)
            return '-';

        return ' ';
    }

    private static string FormatStatus(string status)
    {
        int width = status == "Done" ? 21 : 24;
        return status.PadRight(width);
    }

    private static void RemoveCompletedJobs()
    {
        Jobs.RemoveAll(job => !job.IsRunning);
    }
}
