namespace CodeCrafters.Shell.Commands;

public class FileExecution
{
    public static string? FindInPath(string command)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        if (pathEnv == null)
            return null;

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(dir, command);

            if (File.Exists(fullPath) && IsExecutable(fullPath))
                return fullPath;
        }

        return null;
    }

    public static bool IsExecutable(string path)
    {
        try
        {
            var unixFileMode = File.GetUnixFileMode(path);

            return (unixFileMode & UnixFileMode.UserExecute) != 0 ||
                   (unixFileMode & UnixFileMode.GroupExecute) != 0 ||
                   (unixFileMode & UnixFileMode.OtherExecute) != 0;
        }
        catch
        {
            return false;
        }
    }
}
