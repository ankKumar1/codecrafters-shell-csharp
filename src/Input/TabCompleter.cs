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

        if (text.Contains(' '))
        {

            var tokens = text.Split(' ', StringSplitOptions.None);

            if (tokens.Length >= 2)
            {
                string command = tokens[0];
                string currentWord = tokens[^1];

                string previousWord =
                    tokens.Length >= 3
                        ? tokens[^2]
                        : string.Empty;

                var completions =
                    Completion.GetCompletion(
                        command,
                        currentWord,
                        previousWord);

                if (completions.Count == 1)
                {
                    string completion = completions[0];

                    int startIndex =
                        text.Length - currentWord.Length;

                    string completedText =
                        text[..startIndex] +
                        completion +
                        " ";

                    ReplaceBufferWithText(
                        buffer,
                        completedText);

                    return;
                }
            }
        }

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

        var matches = Utils.Builtins
            .Where(builtin => builtin.StartsWith(text, StringComparison.Ordinal))
            .Concat(FindExecutablesStartingWith(text))
            .Distinct()
            .ToList();

        if (matches.Count == 0)
        {
            BellNoMatch();
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

        var (directoryText, searchDirectory, filePrefix) = SplitFilePrefix(prefix);

        if (!Directory.Exists(searchDirectory))
        {
            BellNoMatch();
            return;
        }

        var matches = FindFileSystemMatches(searchDirectory, filePrefix)
            .Distinct()
            .ToList();

        if (matches.Count == 0)
        {
            BellNoMatch();
            return;
        }

        if (matches.Count == 1)
        {
            string suffix = matches[0].IsDirectory ? "/" : " ";
            ReplaceBufferWithText(buffer, text[..prefixStartIndex] + directoryText + matches[0].Name + suffix);
            ResetTabState();
            return;
        }

        string longestPrefix = LongestCommonPrefix(matches.Select(match => match.Name).ToList());

        if (longestPrefix.Length > filePrefix.Length)
        {
            ReplaceBufferWithText(buffer, text[..prefixStartIndex] + directoryText + longestPrefix);
            ResetTabState();
            return;
        }

        HandleMultipleMatches(buffer, matches);
    }

    private static IEnumerable<FileSystemMatch> FindFileSystemMatches(string searchDirectory, string filePrefix)
    {
        var files = Directory.GetFiles(searchDirectory)
            .Select(file => new FileSystemMatch(Path.GetFileName(file) ?? string.Empty, false));

        var directories = Directory.GetDirectories(searchDirectory)
            .Select(directory => new FileSystemMatch(Path.GetFileName(directory) ?? string.Empty, true));

        return files
            .Concat(directories)
            .Where(match => match.Name.StartsWith(filePrefix, StringComparison.Ordinal));
    }

    private static (string DirectoryText, string SearchDirectory, string FilePrefix) SplitFilePrefix(string prefix)
    {
        int separatorIndex = prefix.LastIndexOfAny(['/', '\\']);

        if (separatorIndex == -1)
            return (string.Empty, Directory.GetCurrentDirectory(), prefix);

        string directoryText = prefix[..(separatorIndex + 1)];
        string filePrefix = prefix[(separatorIndex + 1)..];
        string searchDirectory = Path.GetFullPath(directoryText);

        return (directoryText, searchDirectory, filePrefix);
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

    private void HandleMultipleMatches(StringBuilder buffer, List<FileSystemMatch> matches)
    {
        var formattedMatches = matches
            .OrderBy(match => match.Name, StringComparer.Ordinal)
            .Select(match => match.IsDirectory ? match.Name + "/" : match.Name)
            .ToList();

        HandleMultipleMatches(buffer, formattedMatches);
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

    private void BellNoMatch()
    {
        Console.Write("\x07");
        ResetTabState();
    }

    private sealed record FileSystemMatch(string Name, bool IsDirectory);
}
