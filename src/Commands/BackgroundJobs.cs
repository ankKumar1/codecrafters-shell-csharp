using src.Models;
using System.Diagnostics;

namespace CodeCrafters.Shell.Commands;

public static class BackgroundJobs
{
    private static readonly List<BackgroundJob> Jobs = [];

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

        int jobNumber = GetNextAvailableJobNumber();
        Jobs.Add(new BackgroundJob(jobNumber, process, commandText));

        Console.WriteLine($"[{jobNumber}] {process.Id}");
        return true;
    }

    private static int GetNextAvailableJobNumber()
    {
        var assignedNumbers = Jobs
            .Select(job => job.Number)
            .ToHashSet();

        int jobNumber = 1;

        while (assignedNumbers.Contains(jobNumber))
            jobNumber++;

        return jobNumber;
    }

    public static void ListRunning(TextWriter output)
    {
        var jobsToDisplay = Jobs
            .OrderBy(job => job.Number)
            .ToList();

        if (jobsToDisplay.Count == 0)
            return;

        int? currentJobNumber = jobsToDisplay[^1].Number;
        int? previousJobNumber = jobsToDisplay.Count >= 2
            ? jobsToDisplay[^2].Number
            : null;

        foreach (var job in jobsToDisplay)
        {
            char marker = GetJobMarker(job.Number, currentJobNumber, previousJobNumber);
            WriteJob(output, job, marker);
        }

        RemoveCompletedJobs();
    }

    public static void ReapCompleted(TextWriter output)
    {
        var jobsToReap = Jobs
            .Where(job => !job.IsRunning)
            .OrderBy(job => job.Number)
            .ToList();

        if (jobsToReap.Count == 0)
            return;

        var jobsInStartOrder = Jobs
            .OrderBy(job => job.Number)
            .ToList();

        int? currentJobNumber = jobsInStartOrder[^1].Number;
        int? previousJobNumber = jobsInStartOrder.Count >= 2
            ? jobsInStartOrder[^2].Number
            : null;

        foreach (var job in jobsToReap)
        {
            char marker = GetJobMarker(job.Number, currentJobNumber, previousJobNumber);
            WriteJob(output, job, marker);
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

    private static void WriteJob(TextWriter output, BackgroundJob job, char marker)
    {
        string command = job.IsRunning ? job.Command + " &" : job.Command;
        output.WriteLine($"[{job.Number}]{marker}  {FormatStatus(job.Status)}{command}");
    }

    private static void RemoveCompletedJobs()
    {
        var completedJobs = Jobs
            .Where(job => !job.IsRunning)
            .ToList();

        foreach (var job in completedJobs)
        {
            Jobs.Remove(job);
            job.Process.Dispose();
        }
    }

}
