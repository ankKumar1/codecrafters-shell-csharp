using CodeCrafters.Shell.Commands;
using CodeCrafters.Shell.Input;
using CodeCrafters.Shell.Parsing;

namespace CodeCrafters.Shell;

public sealed class ShellApp
{
    private readonly CommandLineReader _reader = new();
    private readonly CommandExecutor _executor = new();

    public void Run()
    {
        while (true)
        {
            Console.Write("$ ");
            string input = _reader.ReadLine();

            if (input == "exit")
                break;

            var parts = CommandParser.Parse(input);

            if (parts.Count == 0)
                continue;

            if (parts.Contains("|"))
            {
                Pipeline.ExecutePipeline(parts);
                continue;
            }

            var command = parts[0];
            var args = parts.Skip(1).ToArray();

            _executor.Execute(command, args);
        }
    }
}
