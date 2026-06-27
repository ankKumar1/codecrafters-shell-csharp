using CodeCrafters.Shell.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCrafters.Shell.Commands;

public static class DeclareCommand
{
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

            if (!IsValidIdentifier(name))
            {
                Console.Error.WriteLine($"declare: `{arg}': not a valid identifier");
                continue;
            }

            Utils.Variables[name] = value;
        }
    }

    private static void PrintVariable(string name)
    {
        if (Utils.Variables.TryGetValue(name, out var value))
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
        return Utils.Variables.TryGetValue(name, out value!);
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (!(char.IsLetter(name[0]) || name[0] == '_'))
            return false;

        for (int i = 1; i < name.Length; i++)
        {
            if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_'))
                return false;
        }

        return true;
    }
}
