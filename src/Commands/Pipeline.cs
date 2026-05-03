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
        Stream? currentInput = null;

        for (int index = 0; index < stages.Count; index++)
        {
            bool isLastStage = index == stages.Count - 1;
            Stream output = isLastStage
                ? Console.OpenStandardOutput()
                : new MemoryStream();

            var stage = stages[index];
            bool success = ExecuteStage(stage, currentInput ?? Stream.Null, output, isLastStage);

            currentInput?.Dispose();

            if (!success)
            {
                if (output is MemoryStream failedOutput)
                    failedOutput.Dispose();

                return;
            }

            if (output is MemoryStream nextInput)
            {
                nextInput.Position = 0;
                currentInput = nextInput;
            }
            else
            {
                currentInput = null;
            }
        }

        currentInput?.Dispose();
    }

    public static bool IsBuiltin(string command)
    {
        return BuiltinCommands.IsBuiltin(command);
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

    private static bool ExecuteStage(
        List<string> stage,
        Stream input,
        Stream output,
        bool isLastStage)
    {
        string command = stage[0];
        string[] args = stage.Skip(1).ToArray();

        if (IsBuiltin(command))
        {
            ExecuteBuiltinWithStreams(command, args, input, output);
            return true;
        }

        return ExecuteExternalWithStreams(command, args, input, output, isLastStage);
    }

    private static bool ExecuteExternalWithStreams(
        string command,
        string[] args,
        Stream input,
        Stream output,
        bool isLastStage)
    {
        string? path = FileExecution.FindInPath(command);

        if (path == null)
        {
            Console.WriteLine($"{command}: command not found");
            return false;
        }

        bool hasInput = input != Stream.Null;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                RedirectStandardInput = hasInput,
                RedirectStandardOutput = !isLastStage
            }
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();

        if (hasInput)
        {
            input.CopyTo(process.StandardInput.BaseStream);
            process.StandardInput.Close();
        }

        if (!isLastStage)
            process.StandardOutput.BaseStream.CopyTo(output);

        process.WaitForExit();
        return true;
    }

    private static void ExecuteBuiltinWithStreams(
        string command,
        string[] args,
        Stream input,
        Stream output)
    {
        DiscardInput(input);

        using var writer = new StreamWriter(output, leaveOpen: true);
        string argument = string.Join(' ', args);

        switch (command)
        {
            case "echo":
                BuiltinCommands.Echo(argument, writer);
                break;
            case "type":
                BuiltinCommands.Type(argument, writer);
                break;
            case "pwd":
                BuiltinCommands.Pwd(writer);
                break;
            default:
                writer.WriteLine($"{command}: builtin not supported in pipeline");
                break;
        }

        writer.Flush();
    }

    private static void DiscardInput(Stream input)
    {
        if (input == Stream.Null)
            return;

        input.CopyTo(Stream.Null);
    }
}
