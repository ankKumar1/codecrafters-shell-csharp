namespace CodeCrafters.Shell.Input;

public static class CommandHistory
{
    private static readonly List<string> Entries = [];
    private static int _lastPersistedHistoryIndex;

    public static int Count => Entries.Count;

    public static void Add(string input)
    {
        Entries.Add(input);
    }

    public static string Get(int index)
    {
        return Entries[index];
    }

    public static IEnumerable<(int Number, string Command)> GetEntries(int? count = null)
    {
        int startIndex = count.HasValue
            ? Math.Max(0, Entries.Count - count.Value)
            : 0;

        for (int index = startIndex; index < Entries.Count; index++)
            yield return (index + 1, Entries[index]);
    }

    public static void ReadFromFile(string path)
    {
        foreach (var line in File.ReadLines(path))
            Entries.Add(line);

        _lastPersistedHistoryIndex = Entries.Count;
    }

    public static void ReadFromFile(string path, TextWriter output)
    {
        try
        {
            ReadFromFile(path);
        }
        catch (FileNotFoundException)
        {
            output.WriteLine($"history: {path}: No such file or directory");
        }
        catch (DirectoryNotFoundException)
        {
            output.WriteLine($"history: {path}: No such file or directory");
        }
        catch (Exception ex)
        {
            output.WriteLine($"history: {ex.Message}");
        }
    }

    public static void WriteToFile(string path)
    {
        File.WriteAllLines(path, Entries);
        _lastPersistedHistoryIndex = Entries.Count;
    }

    public static void WriteToFile(string path, TextWriter output)
    {
        try
        {
            WriteToFile(path);
        }
        catch (Exception ex)
        {
            output.WriteLine($"history: {ex.Message}");
        }
    }

    public static void AppendToFile(string path)
    {
        File.AppendAllLines(path, Entries.Skip(_lastPersistedHistoryIndex));
        _lastPersistedHistoryIndex = Entries.Count;
    }

    public static void AppendToFile(string path, TextWriter output)
    {
        try
        {
            AppendToFile(path);
        }
        catch (Exception ex)
        {
            output.WriteLine($"history: {ex.Message}");
        }
    }

    public static void LoadFromEnvironment()
    {
        string? historyFile = Environment.GetEnvironmentVariable("HISTFILE");

        if (string.IsNullOrEmpty(historyFile) || !File.Exists(historyFile))
            return;

        try
        {
            ReadFromFile(historyFile);
        }
        catch
        {
        }
    }

    public static void SaveToEnvironmentFile()
    {
        string? historyFile = Environment.GetEnvironmentVariable("HISTFILE");

        if (string.IsNullOrEmpty(historyFile))
            return;

        WriteToFile(historyFile);
    }

    public static void SaveToEnvironmentFile(TextWriter output)
    {
        try
        {
            SaveToEnvironmentFile();
        }
        catch (Exception ex)
        {
            output.WriteLine($"history: {ex.Message}");
        }
    }
}
