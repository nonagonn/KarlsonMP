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
                File.WriteAllText("config", "#port to be used by Riptide\nport=11337\n\n#port to be used by MapDownloader\n#MapDownloader allows users to download maps from the server\n#if you don't know if you should change this, don't\nhttp_port=11338\n");
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
    }
}
