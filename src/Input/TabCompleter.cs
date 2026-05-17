using System.Text;
using CodeCrafters.Shell.Commands;
using CodeCrafters.Shell.Utilities;

namespace CodeCrafters.Shell.Input;

public sealed class TabCompleter
{
    private string? _lastTabPrefix;
    private bool _waitingForSecondTab;

    public void Complete(StringBuilder buffer)
    {
        string text = buffer.ToString();

        int lastSpaceIndex = text.LastIndexOf(' ');

        if (lastSpaceIndex != -1)
        {
            CompleteFilename(buffer, text, lastSpaceIndex + 1);
            return;
        }

        if (_lastTabPrefix != text)
        {
            _waitingForSecondTab = false;
            _lastTabPrefix = text;
        }

        var matches = Utils.AutoCompleteBuiltins
            .Where(builtin => builtin.StartsWith(text, StringComparison.Ordinal))
            .Concat(FindExecutablesStartingWith(text))
            .Distinct()
            .ToList();

        if (matches.Count == 0)
        {
            Console.Write("\x07");
            ResetTabState();
            return;
        }

        if (matches.Count == 1)
        {
            ReplaceBuffer(buffer, matches[0]);
            ResetTabState();
            return;
        }

        string longestPrefix = LongestCommonPrefix(matches);

        if (longestPrefix.Length > text.Length)
        {
            ReplaceBufferWithoutSpace(buffer, longestPrefix);
            ResetTabState();
            return;
        }

        HandleMultipleMatches(buffer, matches);
    }

    private void CompleteFilename(StringBuilder buffer, string text, int prefixStartIndex)
    {
        string prefix = text[prefixStartIndex..];
        string stateKey = $"file:{text}";

        if (_lastTabPrefix != stateKey)
        {
            _waitingForSecondTab = false;
            _lastTabPrefix = stateKey;
        }

        var matches = Directory.GetFiles(Directory.GetCurrentDirectory())
            .Select(file => Path.GetFileName(file) ?? string.Empty)
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Distinct()
            .ToList();

        if (matches.Count == 0)
        {
            Console.Write("\x07");
            ResetTabState();
            return;
        }

        if (matches.Count == 1)
        {
            ReplaceBufferWithText(buffer, text[..prefixStartIndex] + matches[0] + " ");
            ResetTabState();
            return;
        }

        string longestPrefix = LongestCommonPrefix(matches);

        if (longestPrefix.Length > prefix.Length)
        {
            ReplaceBufferWithText(buffer, text[..prefixStartIndex] + longestPrefix);
            ResetTabState();
            return;
        }

        HandleMultipleMatches(buffer, matches);
    }

    private void HandleMultipleMatches(StringBuilder buffer, List<string> matches)
    {
        matches.Sort(StringComparer.Ordinal);

        if (!_waitingForSecondTab)
        {
            Console.Write("\x07");
            _waitingForSecondTab = true;
            return;
        }

        Console.WriteLine();
        Console.WriteLine(string.Join("  ", matches));

        Console.Write("$ ");
        Console.Write(buffer.ToString());

        _waitingForSecondTab = false;
    }

    private static void ReplaceBuffer(StringBuilder buffer, string completion)
    {
        ClearBuffer(buffer);

        buffer.Append(completion);
        buffer.Append(' ');
        Console.Write(completion + " ");
    }

    private static void ReplaceBufferWithoutSpace(StringBuilder buffer, string completion)
    {
        ClearBuffer(buffer);

        buffer.Append(completion);
        Console.Write(completion);
    }

    private static void ReplaceBufferWithText(StringBuilder buffer, string text)
    {
        ClearBuffer(buffer);

        buffer.Append(text);
        Console.Write(text);
    }

    private static void ClearBuffer(StringBuilder buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            Console.Write("\b \b");

        buffer.Clear();
    }

    private static List<string> FindExecutablesStartingWith(string prefix)
    {
        var results = new HashSet<string>();
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        if (pathEnv == null)
            return results.ToList();

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (!Directory.Exists(dir))
                continue;

            try
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    var name = Path.GetFileName(file);

                    if (!name.StartsWith(prefix, StringComparison.Ordinal))
                        continue;

                    if (FileExecution.IsExecutable(file))
                        results.Add(name);
                }
            }
            catch
            {
                throw;
            }
        }

        return results.ToList();
    }

    private static string LongestCommonPrefix(List<string> items)
    {
        if (items.Count == 0)
            return string.Empty;

        string prefix = items[0];

        foreach (var item in items)
        {
            int i = 0;

            while (i < prefix.Length &&
                   i < item.Length &&
                   prefix[i] == item[i])
            {
                i++;
            }

            prefix = prefix[..i];

            if (prefix.Length == 0)
                break;
        }

        return prefix;
    }

    private void ResetTabState()
    {
        _waitingForSecondTab = false;
        _lastTabPrefix = null;
    }
}
