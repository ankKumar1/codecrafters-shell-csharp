using System.Text.RegularExpressions;

namespace CodeCrafters.Shell.Utilities;

public static class Utils
{
    public static readonly string[] Builtins = ["echo", "type", "exit", "pwd", "cd", "history", "jobs", "complete", "declare"];
    public static readonly Dictionary<string, string> Variables = [];

    private static readonly Regex VariableRegex = new(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}|\$([A-Za-z_][A-Za-z0-9_]*)");

    public static string Expand(string text)
    {
        return VariableRegex.Replace(text, match =>
        {
            string name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;  

            return Variables.TryGetValue(name, out var value) ? value : string.Empty;
        });
    }

    public static bool IsPureVariableReference(string text)
    {
        return VariableRegex.IsMatch(text);
    }
}
