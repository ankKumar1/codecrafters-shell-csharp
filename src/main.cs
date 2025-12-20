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

            Console.WriteLine($"{command}: command not found");
        }

    }
}
