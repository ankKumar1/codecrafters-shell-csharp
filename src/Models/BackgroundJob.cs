using System.Diagnostics;

namespace src.Models;

public sealed class BackgroundJob
{
    public BackgroundJob(int number, Process process, string command)
    {
        Number = number;
        Process = process;
        Command = command;
    }

    public int Number { get; }
    public Process Process { get; }
    public int ProcessId => Process.Id;
    public string Command { get; }
    public string Status => Process.HasExited ? "Done" : "Running";
    public bool IsRunning => !Process.HasExited;
}
