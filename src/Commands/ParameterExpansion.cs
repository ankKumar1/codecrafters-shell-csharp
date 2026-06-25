using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCrafters.Shell.Commands;

public static class DeclareCommand
{
    private static readonly Dictionary<string, string> _variables = [];

    public static void Declare(string[] args)
    {
        if (args.Length == 0)
            return;

        if (args[0] == "-p" && args.Length >= 2)
        {
            PrintVariable(args[1]);
            return;
        }

        foreach (var arg in args)
        {
            int index = arg.IndexOf('=');

            if (index <= 0)
                continue;

            string name = arg[..index];
            string value = arg[(index + 1)..];

            _variables[name] = value;
        }
    }

    private static void PrintVariable(string name)
    {
        if (_variables.TryGetValue(name, out var value))
        {
            Console.WriteLine($"declare -- {name}=\"{value}\"");
        }
        else
        {
            Console.Error.WriteLine($"declare: {name}: not found");
        }
    }

    public static bool TryGetVariable(string name, out string value)
    {
        return _variables.TryGetValue(name, out value!);
    }
}
