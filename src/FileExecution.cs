using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCrafters.Shell.Commands;
public class FileExecution
{
    public static string? FindInPath(string command)
    {
        // Get the PATH environment variable
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        // Return null if PATH is not set
        if (pathEnv == null)
            return null;

        // Split PATH into individual directories
        var directories = pathEnv.Split(Path.PathSeparator);

        // Search each directory for the command
        foreach (var dir in directories)
        {
            // Construct the full path to the potential executable
            var fullPath = Path.Combine(dir, command);

            // Check if file exists and is executable
            if (File.Exists(fullPath) && IsExecutable(fullPath))
            {
                return fullPath;
            }
        }

        // Command not found in any PATH directory
        return null;
    }

    public static bool IsExecutable(string path)
    {
        try
        {
            // Get Unix file permissions (only available on Unix-like systems)
            var unixFileMode = File.GetUnixFileMode(path);
            // Check if any execute permission is set (user, group, or other)
            return (unixFileMode & UnixFileMode.UserExecute) != 0 ||
                   (unixFileMode & UnixFileMode.GroupExecute) != 0 ||
                   (unixFileMode & UnixFileMode.OtherExecute) != 0;
        }
        catch
        {
            // If unable to check permissions, assume not executable
            return false;
        }
    }
}