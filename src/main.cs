class Program
{
    static void Main()
    {
        // TODO: Uncomment the code below to pass the first stage
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine();

            if (command == "exit")
            {
                break;
            }

            if (command != null)
            {
                int idx = command.IndexOf(' ');

                string cmd = idx == -1 ? command : command.Substring(0, idx);
                string args = idx == -1 ? string.Empty : command.Substring(idx + 1).Trim();

                ExecuteCommand(cmd, args);
            }
        }

    }

    public static void ExecuteCommand(string command, string args)
    {
        if (command == "echo")
        {
            Console.WriteLine(args);
        }
        else
        {
            Console.WriteLine($"{command}: command not found");
        }
    }
}
