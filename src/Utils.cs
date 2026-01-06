using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCrafters.Shell.Utilities;
public static class Utils
{
    public static readonly string[] Builtins = ["echo", "type", "exit", "pwd", "cd"];
    public static readonly string[] AutoCompleteBuiltins = ["echo", "exit"];
}

