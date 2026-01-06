using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeCrafters.Shell.Utilities;

namespace CodeCrafters.Shell.Commands;
public class Pipeline
{
    public static void ExecutePipeline(List<string> left, List<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            Console.WriteLine("Invalid pipeline");
            return;
        }

        string leftCmd = left[0];
        string[] leftArgs = left.Skip(1).ToArray();

        string rightCmd = right[0];
        string[] rightArgs = right.Skip(1).ToArray();

        using var pipe = new MemoryStream();

        // 🔹 LEFT SIDE
        if (IsBuiltin(leftCmd))
        {
            ExecuteBuiltinWithStreams(
                leftCmd,
                leftArgs,
                Stream.Null,
                pipe
            );
        }
        else
        {
            string? leftPath = FileExecution.FindInPath(leftCmd);
            if (leftPath == null)
            {
                Console.WriteLine($"{leftCmd}: command not found");
                return;
            }

            var leftProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = leftPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            foreach (var arg in leftArgs)
                leftProcess.StartInfo.ArgumentList.Add(arg);

            leftProcess.Start();
            leftProcess.StandardOutput.BaseStream.CopyTo(pipe);
            leftProcess.WaitForExit();
        }

        pipe.Position = 0;

        // 🔹 RIGHT SIDE
        if (IsBuiltin(rightCmd))
        {
            ExecuteBuiltinWithStreams(
                rightCmd,
                rightArgs,
                pipe,
                Console.OpenStandardOutput()
            );
        }
        else
        {
            string? rightPath = FileExecution.FindInPath(rightCmd);
            if (rightPath == null)
            {
                Console.WriteLine($"{rightCmd}: command not found");
                return;
            }

            var rightProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = rightPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true
                }
            };

            foreach (var arg in rightArgs)
                rightProcess.StartInfo.ArgumentList.Add(arg);

            rightProcess.Start();
            pipe.CopyTo(rightProcess.StandardInput.BaseStream);
            rightProcess.StandardInput.Close();
            rightProcess.WaitForExit();
        }
    }


    public static bool IsBuiltin(string command)
    {
        return Utils.Builtins.Contains(command);
    }

    static void ExecuteBuiltinWithStreams(
    string command,
    string[] args,
    Stream input,
    Stream output)
    {
        using var writer = new StreamWriter(output, leaveOpen: true);
        using var reader = new StreamReader(input);

        if (command == "echo")
        {
            writer.WriteLine(string.Join(' ', args));
        }
        else if (command == "type")
        {
            string arg = args.FirstOrDefault() ?? "";
            if (Utils.Builtins.Contains(arg))
                writer.WriteLine($"{arg} is a shell builtin");
            else
            {
                string? path = FileExecution.FindInPath(arg);
                if (path != null)
                    writer.WriteLine($"{arg} is {path}");
                else
                    writer.WriteLine($"{arg}: not found");
            }
        }
        else if (command == "pwd")
        {
            writer.WriteLine(Directory.GetCurrentDirectory());
        }
        else
        {
            writer.WriteLine($"{command}: builtin not supported in pipeline");
        }

        writer.Flush();
    }


}

