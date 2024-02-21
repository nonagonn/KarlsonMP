using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public static class CommandManager
    {
        private static Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();

        public static void Init()
        {
            commands.Add("exit", (_) =>
            {
                Console.WriteLine("Quitting server..");
                MapDownloader.Exit();
                Program.ExitServer();
            });
            commands.Add("map", (args) =>
            {
                if (args.Length != 2) Console.WriteLine("map [mapname] - change map to [mapname]");
                else MapManager.LoadMap(args[1]);
            });

            commands.Add("cmds", (_) =>
            {
                Console.WriteLine("List of commands:");
                foreach (var x in commands)
                    Console.WriteLine(x.Key);
            });
        }

        public static void Execute(string label, string[] args)
        {
            if(!commands.ContainsKey(label))
                Console.WriteLine($"Unknown command {label}. Run 'cmds' for a list of commands");
            else
                commands[label](args);
        }
    }
}
