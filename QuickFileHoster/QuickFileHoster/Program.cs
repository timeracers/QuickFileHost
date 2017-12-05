using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace QuickFileHoster
{
    public static class Program
    {
        private static Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>();
        private static Dictionary<string, string[]> CommandsHelp = new Dictionary<string, string[]>();

        private static void Add(string name, Action<string[]> command, params string[] help)
        {
            Commands.Add(name.ToUpper(), command);
            CommandsHelp.Add(name.ToUpper(), help);
        }

        static void Main(string[] args)
        {
            Console.Title = "Quick File Hoster";
            if (IsAdmin())
                Console.Title = "Administrator: " + Console.Title;
            if (!IsAdmin())
                Console.WriteLine("Administrator privileges are required to host a server");
            Add("HOST", (s) => new Server().Host(s), "Syntax: Host <Port> <IO File Path> [<Password>] [<IO File Path>] [<IO File Path>]...",
                "Hosts files to the local ip with specified password. To download: index in where 0 is the first element.",
                "This will cache files, which will allow you to delete them and have this continue to work.");
            Add("HOSTFOLDER", (s) => new Server().HostFolder(s), "Syntax: Hostfolder <Port> <Folder> [<Password>]",
                "Recursively hosts all files of the specified folder to the local ip with the specified password.",
                "To download: specify the file location relative to the folder.",
                "This won't cache files, which means changes to them will be reflected to users.");
            Add("EXIT", (s) => notQuitting = false, "Syntax: Exit", "Exits the program");
            Add("QUIT", (s) => notQuitting = false, "Syntax: Quit", "Exits the program");
            Add("Clear", (s) => Console.Clear(), "Syntax: Clear", "Clears the console");
            Add("HELP", (s) =>
            {
                if (s.Length == 0)
                {
                    Console.WriteLine("Valid Commands:");
                    foreach (var help in CommandsHelp)
                        WriteHelpSection(help);
                }
                else
                {
                    foreach (var command in s)
                    {
                        if (Commands.ContainsKey(command.ToUpper()))
                            WriteHelpSection(new KeyValuePair<string, string[]>(command.ToUpper(), CommandsHelp[command.ToUpper()]));
                        else
                            Console.WriteLine(command + " not recognized");
                    }
                }
            }, "Syntax: Help", "Writes the help section of each command",
                "Syntax: Help <Command> [<Command>] [<Command>]...", "Writes the help section of those commands");

            if (args.Length > 0)
            {
                Console.WriteLine("Starting with specified command");
                ResolveCommand(args);
            }
            Console.WriteLine("Started, you can now type commands");

            while (notQuitting)
                ResolveCommand(ReadCommandLineArgs(Console.ReadLine()));
            Environment.Exit(0);
        }

        private static bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void ResolveCommand(string[] input)
        {
            var commandFound = Commands.ContainsKey(input[0].ToUpper());
            if (commandFound)
                Commands[input[0].ToUpper()](input.SubArray(1, input.Length - 1));
            else
                Console.WriteLine("Invalid Command, type help to display all commands with their help sections");
        }

        private static string[] ReadCommandLineArgs(string input)
        {
            if (input.IndexOf(" ") == -1)
                return new[] { input };
            else
            {
                var inputs = new List<string>() { input.Substring(0, input.IndexOf(" ")) };
                var remains = input.Substring(input.IndexOf(" "));
                while (remains.Length > 1)
                {
                    if (remains.Substring(0, 2) == " \"")
                    {
                        remains = remains.Substring(2);
                        var index = remains.IndexOf("\"");
                        inputs.Add(index != -1 ? remains.Substring(0, index) : remains);
                        remains = index != -1 ? remains.Substring(index + 1) : "";
                    }
                    else
                    {
                        remains = remains.Substring(1);
                        var index = remains.IndexOf(" ");
                        inputs.Add(index != -1 ? remains.Substring(0, index) : remains);
                        remains = index != -1 ? remains.Substring(index) : "";
                    }
                }
                return inputs.ToArray();
            }
        }

        private static void WriteHelpSection(KeyValuePair<string, string[]> help)
        {
            Console.WriteLine("    " + help.Key);
            foreach (var st in help.Value)
                Console.WriteLine(st);
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private static bool notQuitting = true;
    }
}
