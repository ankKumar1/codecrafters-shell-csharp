using System.Text;

namespace CodeCrafters.Shell.Input;

public sealed class CommandLineReader
{
    private readonly TabCompleter _tabCompleter = new();

    public string ReadLine()
    {
        var buffer = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (key.Key == ConsoleKey.Tab)
            {
                _tabCompleter.Complete(buffer);
                continue;
            }

            Console.Write(key.KeyChar);
            buffer.Append(key.KeyChar);
        }

        return buffer.ToString();
    }
}
