using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                Program.ExitServer();
                MapDownloader.Exit();
            });
            commands.Add("map", (args) =>
            {
                if (args.Length != 2) Console.WriteLine("map [mapname] - change map to [mapname]");
                else MapManager.LoadMap(args[1]);
            });
            commands.Add("maps", (args) =>
            {
                Console.WriteLine("Default maps:");
                foreach (var x in MapManager.defaultMaps)
                    Console.WriteLine(x.name);
                Console.WriteLine("Custom maps:");
                foreach(var x in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Maps")))
                    if(x.EndsWith(".kme_raw") && File.Exists(x.Replace(".kme_raw", ".kme_data")))
                        Console.WriteLine(Path.GetFileNameWithoutExtension(x));
            });
            commands.Add("gamemode", (args) =>
            {
                if (args.Length != 2) Console.WriteLine("gamemode [mode] - change gamemode to [mode]");
                else
                {
                    GamemodeManager.SafeCall(GamemodeManager.currentGamemode!.OnStop);
                    if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Gamemodes", args[1] + ".gmf")))
                    {
                        Console.WriteLine("[ERROR] Couldn't find gamemode " + args[1] + ".gmf");
                        return;
                    }
                    var asm = AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Gamemodes", args[1] + ".gmf")));
                    var type = asm.GetTypes().Where(x => x.BaseType == typeof(GamemodeApi.Gamemode)).FirstOrDefault();
                    if (type == null)
                    {
                        Console.WriteLine("[ERROR] Couldn't find gamemode entrypoint");
                        return;
                    }
                    GamemodeManager.currentGamemode = (GamemodeApi.Gamemode)Activator.CreateInstance(type, null)!;
                    GamemodeManager.SafeCall(GamemodeManager.currentGamemode!.OnStart);
                    // re-handshake all players to gamemode
                    foreach (var i in NetworkManager.registeredOnGamemode)
                        GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new GamemodeApi.MessageClientToServer.MessageHandshake(i, NetworkManager.usernameDatabase[i])));
                }
            });
            commands.Add("reload", _ =>
            {
                GamemodeManager.SafeCall(GamemodeManager.currentGamemode!.OnStop);
                GamemodeManager.SafeCall(GamemodeManager.currentGamemode!.OnStart);
                // re-handshake all players to gamemode
                foreach (var i in NetworkManager.registeredOnGamemode)
                    GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new GamemodeApi.MessageClientToServer.MessageHandshake(i, NetworkManager.usernameDatabase[i])));
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
