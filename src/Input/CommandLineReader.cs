using System.Text;
using CodeCrafters.Shell.Utilities;

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
        _historyIndex = Utils.history.Count;
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
        if (Utils.history.Count == 0)
            return;

        if (_historyIndex == Utils.history.Count)
            _currentDraft = buffer.ToString();

        if (_historyIndex > 0)
            _historyIndex--;

        ReplaceBuffer(buffer, Utils.history[_historyIndex]);
    }

    private void ShowNextHistoryEntry(StringBuilder buffer)
    {
        if (Utils.history.Count == 0 || _historyIndex == Utils.history.Count)
            return;

        if (_historyIndex < Utils.history.Count - 1)
        {
            _historyIndex++;
            ReplaceBuffer(buffer, Utils.history[_historyIndex]);
            return;
        }

        _historyIndex = Utils.history.Count;
        ReplaceBuffer(buffer, _currentDraft);
    }

    private void ResetHistoryNavigation(StringBuilder buffer)
    {
        _historyIndex = Utils.history.Count;
        _currentDraft = buffer.ToString();
    }

    private static void AddToHistory(string input)
    {
        Utils.history.Add(input);
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
