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
                File.WriteAllText("config", "#port to be used by Riptide\nport=11337\n\n#startup gamemode\ngamemode=FFA");
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
                    case "gamemode":
                        GAMEMODE = split[1].Trim();
                        break;
                    case "max_players":
                        MAX_PLAYERS = ushort.Parse(split[1].Trim());
                        break;
                    case "motd":
                        MOTD = NetworkManager.MOTD = split[1].Trim();
                        break;
                    default:
                        Console.WriteLine($"[ERROR] Found unknown key in config '{split[0]}'");
                        Console.WriteLine($"[ERROR] Line: '{line}'");
                        break;
                }
            }
        }

        public static int TPS { get; private set; } = 50; // match TPS with Unity's Time.fixedDeltaTime
        public static int MSPT => 1000 / TPS;
        public static ushort PORT { get; private set; } = 11337;
        public static string GAMEMODE { get; private set; } = "FFA";
        public static ushort MAX_PLAYERS { get; private set; } = 16;
        public static string MOTD = "<MOTD>";
    }
}
