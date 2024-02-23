using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public class GamemodeManager
    {
        public static GamemodeApi.Gamemode currentGamemode;
        public static void SafeCall(Action call)
        {
            try
            {
                call();
            }
            catch (Exception e)
            {
                Console.WriteLine("[GAMEMODE ERROR] " + e.ToString());
            }
        }

        public static void Init()
        {
            if(!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Gamemodes")))
            {
                Console.WriteLine("[ERROR] Missing 'Gamemodes' directory.");
                Process.GetCurrentProcess().Kill();
                return;
            }
            if(!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Gamemodes", Config.GAMEMODE + ".dll")))
            {
                Console.WriteLine($"[ERROR] File '{Config.GAMEMODE}.dll' doesn't exist in gamemodes directory.");
                Console.WriteLine($"[ERROR] Please check that you set the correct name in config");
                Process.GetCurrentProcess().Kill();
                return;
            }
            var asm = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), "Gamemodes", Config.GAMEMODE + ".dll"));
            Type t = (from x in asm.GetTypes()
                      where x.BaseType == typeof(GamemodeApi.Gamemode)
                      select x).FirstOrDefault();
            if (t == null)
            {
                Console.WriteLine("[ERROR] Couldn't find base type in gamemode.");
                Console.WriteLine("[ERROR] Make sure at least a class inherits the `ServerKMP.GamemodeApi.Gamemode` type");
                Process.GetCurrentProcess().Kill();
                return;
            }
            currentGamemode = (GamemodeApi.Gamemode)Activator.CreateInstance(t, null);
            if (currentGamemode == null)
            {
                Console.WriteLine("[ERROR] Gamemode loading failed.");
                Console.WriteLine("[ERROR] Couldn't instantiate base type.");
                Process.GetCurrentProcess().Kill();
                return;
            }
            SafeCall(currentGamemode.OnStart);
        }
    }
}
