using System.Diagnostics;

namespace CodeCrafters.Shell.Commands;

public class Pipeline
{
    public static void ExecutePipeline(List<string> parts)
    {
        var stages = SplitIntoStages(parts);

        if (stages.Count < 2 || stages.Any(stage => stage.Count == 0))
        {
            Console.WriteLine("Invalid pipeline");
            return;
        }

        ExecutePipeline(stages);
    }

    public static void ExecutePipeline(List<List<string>> stages)
    {
        var pipelineStages = CreatePipelineStages(stages);

        if (pipelineStages == null)
            return;

        try
        {
            StartExternalStages(pipelineStages);

            var copyTasks = StartStreamCopies(pipelineStages);
            var builtinTasks = StartBuiltinStages(pipelineStages);

            WaitForLastStage(pipelineStages, builtinTasks);
            StopRunningProcesses(pipelineStages);

            Task.WaitAll([.. copyTasks, .. builtinTasks]);
        }
        finally
        {
            DisposeProcesses(pipelineStages);
        }
    }

    public static bool IsBuiltin(string command)
    {
        return BuiltinCommands.IsBuiltin(command);
    }

    private static List<PipelineStage>? CreatePipelineStages(List<List<string>> stages)
    {
        var pipelineStages = new List<PipelineStage>();

        foreach (var stage in stages)
        {
            string command = stage[0];
            string[] args = stage.Skip(1).ToArray();

            if (IsBuiltin(command))
            {
                pipelineStages.Add(PipelineStage.ForBuiltin(command, args));
                continue;
            }

            string? path = FileExecution.FindInPath(command);

            if (path == null)
            {
                Console.WriteLine($"{command}: command not found");
                return null;
            }

            pipelineStages.Add(PipelineStage.ForExternal(command, args, path));
        }

        return pipelineStages;
    }

    private static void StartExternalStages(List<PipelineStage> stages)
    {
        for (int index = 0; index < stages.Count; index++)
        {
            var stage = stages[index];

            if (stage.IsBuiltin)
                continue;

            bool hasPrevious = index > 0;
            bool hasNext = index < stages.Count - 1;

            stage.Process = CreateProcess(stage, hasPrevious, hasNext);
            stage.Process.Start();
        }
    }

    private static Process CreateProcess(PipelineStage stage, bool hasPrevious, bool hasNext)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = stage.Path,
                UseShellExecute = false,
                RedirectStandardInput = hasPrevious,
                RedirectStandardOutput = hasNext
            }
        };

        foreach (var arg in stage.Args)
            process.StartInfo.ArgumentList.Add(arg);

        return process;
    }

    private static List<Task> StartStreamCopies(List<PipelineStage> stages)
    {
        var copyTasks = new List<Task>();

        for (int index = 0; index < stages.Count - 1; index++)
        {
            var current = stages[index];
            var next = stages[index + 1];

            if (current.IsBuiltin)
                continue;

            if (next.IsBuiltin)
            {
                CloseUnusedOutput(current);
                continue;
            }

            copyTasks.Add(CopyToNextStage(current, next));
        }

        return copyTasks;
    }

    private static List<Task> StartBuiltinStages(List<PipelineStage> stages)
    {
        var builtinTasks = new List<Task>();

        for (int index = 0; index < stages.Count; index++)
        {
            var stage = stages[index];

            if (!stage.IsBuiltin)
                continue;

            Stream output = GetBuiltinOutputStream(stages, index);
            bool closeOutput = index < stages.Count - 1;
            builtinTasks.Add(Task.Run(() => ExecuteBuiltin(stage.Command, stage.Args, output, closeOutput)));
        }

        return builtinTasks;
    }

    private static Task CopyToNextStage(PipelineStage current, PipelineStage next)
    {
        return Task.Run(() =>
        {
            try
            {
                current.Process!.StandardOutput.BaseStream.CopyTo(next.Process!.StandardInput.BaseStream);
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                CloseStandardInput(next);
            }
        });
    }

    private static Stream GetBuiltinOutputStream(List<PipelineStage> stages, int index)
    {
        if (index == stages.Count - 1)
            return Console.OpenStandardOutput();

        var next = stages[index + 1];

        if (next.IsBuiltin)
            return Stream.Null;

        return next.Process!.StandardInput.BaseStream;
    }

    private static void ExecuteBuiltin(string command, string[] args, Stream output, bool closeOutput)
    {
        try
        {
            using var writer = new StreamWriter(output, leaveOpen: true);

            if (!BuiltinCommands.Execute(command, args, writer))
                writer.WriteLine($"{command}: builtin not supported in pipeline");

            writer.Flush();
        }
        catch (IOException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            if (closeOutput)
                output.Close();
        }
    }

    private static void WaitForLastStage(List<PipelineStage> stages, List<Task> builtinTasks)
    {
        var lastStage = stages[^1];

        if (lastStage.IsBuiltin)
        {
            Task.WaitAll([.. builtinTasks]);
            return;
        }

        lastStage.Process!.WaitForExit();
    }

    private static void StopRunningProcesses(List<PipelineStage> stages)
    {
        foreach (var stage in stages)
        {
            if (stage.Process == null || stage.Process.HasExited)
                continue;

            try
            {
                stage.Process.Kill();
            }
            catch
            {
            }
        }
    }

    private static void DisposeProcesses(List<PipelineStage> stages)
    {
        foreach (var stage in stages)
            stage.Process?.Dispose();
    }

    private static void CloseUnusedOutput(PipelineStage stage)
    {
        try
        {
            stage.Process!.StandardOutput.Close();
        }
        catch
        {
        }
    }

    private static void CloseStandardInput(PipelineStage stage)
    {
        try
        {
            stage.Process!.StandardInput.Close();
        }
        catch
        {
        }
    }

    private static List<List<string>> SplitIntoStages(List<string> parts)
    {
        var stages = new List<List<string>>();
        var current = new List<string>();

        foreach (var part in parts)
        {
            if (part == "|")
            {
                stages.Add(current);
                current = [];
                continue;
            }

            current.Add(part);
        }

        stages.Add(current);
        return stages;
    }

    private sealed class PipelineStage
    {
        private PipelineStage(string command, string[] args, string? path)
        {
            Command = command;
            Args = args;
            Path = path;
        }

        public string Command { get; }
        public string[] Args { get; }
        public string? Path { get; }
        public Process? Process { get; set; }
        public bool IsBuiltin => Path == null;

        public static PipelineStage ForBuiltin(string command, string[] args)
        {
            return new PipelineStage(command, args, null);
        }

        public static PipelineStage ForExternal(string command, string[] args, string path)
        {
            return new PipelineStage(command, args, path);
        }
    }
}
