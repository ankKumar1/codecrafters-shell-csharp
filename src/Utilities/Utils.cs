namespace CodeCrafters.Shell.Utilities;

public static class Utils
{
    public static readonly string[] Builtins = ["echo", "type", "exit", "pwd", "cd", "history"];
    public static readonly string[] AutoCompleteBuiltins = ["echo", "exit"];
    public static List<string> history = new List<string>();
    public static int LastPersistedHistoryIndex = 0;
}
