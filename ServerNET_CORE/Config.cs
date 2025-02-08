using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public static class Config
    {
        public static void LoadConfig()
        {
            if (!File.Exists("config"))
                File.WriteAllText("config", "#port to be used by Riptide\nport=11337\n\n#port to be used by MapDownloader\n#MapDownloader allows users to download maps from the server\n#if you don't know if you should change this, don't\nhttp_port=11338\n\n#startup gamemode\ngamemode=FFA\n\n#api url for discord authentication\n#only change this if you know what you are doing\ndiscord_api=https://karlsonlevelloader.000webhostapp.com/kmp");
            string[] lines = File.ReadAllLines("config");
            foreach (var line in lines)
            {
                if (line.Length == 0 || line.Trim().Length == 0 || line.StartsWith("#")) continue;
                var split = line.Split('=');
                switch (split[0].Trim())
                {
                    case "tps":
                        TPS = int.Parse(split[1].Trim());
                        break;
                    case "port":
                        PORT = ushort.Parse(split[1].Trim());
                        break;
                    case "http_port":
                        HTTP_PORT = ushort.Parse(split[1].Trim());
                        break;
                    case "gamemode":
                        GAMEMODE = split[1].Trim();
                        break;
                    case "discord_api":
                        DISCORD_API = split[1].Trim();
                        break;
                    case "max_players":
                        MAX_PLAYERS = ushort.Parse(split[1].Trim());
                        break;
                    case "anno":
                        ANNO = true;
                        break;
                    case "ignore_discord":
                        IGNORE_DISCORD = true;
                        break;
                    default:
                        Console.WriteLine($"[ERROR] Found unknown key in config '{split[0]}'");
                        Console.WriteLine($"[ERROR] Line: '{line}'");
                        break;
                }
            }
        }

        public static int TPS { get; private set; } = 120; // TPS doesn't reaaaaly matter that much
        // unity runs around ~160 tps, 120 is good enough, because you get throttled anyway
        public static int MSPT => 1000 / TPS;
        public static ushort PORT { get; private set; } = 11337;
        public static ushort HTTP_PORT { get; private set; } = 11338;
        public static string GAMEMODE { get; private set; } = "FFA";
        public static string DISCORD_API { get; private set; } = "https://karlsonlevelloader.000webhostapp.com/kmp";
        public static string API_KEY { get; private set; } = "karlsonmp!_sex123$";
        public static ushort MAX_PLAYERS { get; private set; } = 16;
        public static bool ANNO { get; private set; } = false;
        public static bool IGNORE_DISCORD { get; private set; } = false;
    }
}
