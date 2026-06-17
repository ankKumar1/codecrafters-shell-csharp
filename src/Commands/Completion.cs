using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCrafters.Shell.Commands;

public class Completion
{
    private static readonly Dictionary<string, string> _completions = new();

    public static void Complete(string[] args)
    {
        if (args.Length == 0)
            return;

        if (args[0] == "-C" && args.Length >= 3)
        {
            string script = args[1];
            string command = args[2];

            _completions[command] = script;
            return;
        }

        if (args[0] == "-p" && args.Length >= 2)
        {
            string command = args[1];

            if (_completions.TryGetValue(command, out var script))
            {
                Console.WriteLine($"complete -C '{script}' {command}");
            }
            else
            {
                Console.Error.WriteLine($"complete: {command}: no completion specification");
            }
        }
    }

    public static string? GetCompletion(string command)
    {
        if (!_completions.TryGetValue(command, out var script))
            return null;

        try
        {
            using var output = new MemoryStream();

            ExternalProgramRunner.Run(
                path: script,
                commandName: command,
                args: Array.Empty<string>(),
                output: output);

            output.Position = 0;

            using var reader = new StreamReader(output);

            return reader.ReadLine();
        }
        catch
        {
            return null;
        }
    }

}

