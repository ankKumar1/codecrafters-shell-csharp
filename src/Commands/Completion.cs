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

    public static List<string> GetCompletion(
    string command,
    string currentWord,
    string previousWord,
    string line)
    {
        if (!_completions.TryGetValue(command, out var script))
            return [];

        try
        {
            using var output = new MemoryStream();

            ExternalProgramRunner.Run(
                path: script,
                commandName: command,
                args:
                [
                    command,
                currentWord,
                previousWord
                ],
                output: output,
                 environment: new Dictionary<string, string>
                 {
                     ["COMP_LINE"] = line,
                     ["COMP_POINT"] = Encoding.UTF8.GetByteCount(line).ToString()
                 });

            output.Position = 0;

            using var reader = new StreamReader(output);

            List<string> completions = [];

            while (!reader.EndOfStream)
            {
                string? words = reader.ReadLine();

                if (!string.IsNullOrWhiteSpace(words))
                    completions.Add(words);
            }

            return completions;
        }
        catch
        {
            return [];
        }
    }

}

