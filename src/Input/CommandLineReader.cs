using System.Text;

namespace CodeCrafters.Shell.Input;

public sealed class CommandLineReader
{
    private readonly TabCompleter _tabCompleter = new();
    private int _historyIndex;
    private string _currentDraft = string.Empty;

    public string ReadLine()
    {
        if (Console.IsInputRedirected)
        {
            string redirectedInput = Console.ReadLine() ?? string.Empty;
            AddToHistory(redirectedInput);
            return redirectedInput;
        }

        var buffer = new StringBuilder();
        _historyIndex = CommandHistory.Count;
        _currentDraft = string.Empty;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.UpArrow)
            {
                ShowPreviousHistoryEntry(buffer);
                continue;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                ShowNextHistoryEntry(buffer);
                continue;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                    Console.Write("\b \b");
                    ResetHistoryNavigation(buffer);
                }

                continue;
            }

            if (key.Key == ConsoleKey.Tab)
            {
                _tabCompleter.Complete(buffer);
                ResetHistoryNavigation(buffer);
                continue;
            }

            Console.Write(key.KeyChar);
            buffer.Append(key.KeyChar);
            ResetHistoryNavigation(buffer);
        }

        string input = buffer.ToString();
        AddToHistory(input);
        return input;
    }

    private void ShowPreviousHistoryEntry(StringBuilder buffer)
    {
        if (CommandHistory.Count == 0)
            return;

        if (_historyIndex == CommandHistory.Count)
            _currentDraft = buffer.ToString();

        if (_historyIndex > 0)
            _historyIndex--;

        ReplaceBuffer(buffer, CommandHistory.Get(_historyIndex));
    }

    private void ShowNextHistoryEntry(StringBuilder buffer)
    {
        if (CommandHistory.Count == 0 || _historyIndex == CommandHistory.Count)
            return;

        if (_historyIndex < CommandHistory.Count - 1)
        {
            _historyIndex++;
            ReplaceBuffer(buffer, CommandHistory.Get(_historyIndex));
            return;
        }

        _historyIndex = CommandHistory.Count;
        ReplaceBuffer(buffer, _currentDraft);
    }

    private void ResetHistoryNavigation(StringBuilder buffer)
    {
        _historyIndex = CommandHistory.Count;
        _currentDraft = buffer.ToString();
    }

    private static void AddToHistory(string input)
    {
        CommandHistory.Add(input);
    }

    private static void ReplaceBuffer(StringBuilder buffer, string value)
    {
        for (int i = 0; i < buffer.Length; i++)
            Console.Write("\b \b");

        buffer.Clear();
        buffer.Append(value);
        Console.Write(value);
    }
}
