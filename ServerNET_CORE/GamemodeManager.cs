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
        public static GamemodeApi.Gamemode? currentGamemode;
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
            if(Config.GAMEMODE == "FFA")
            {
                currentGamemode = new Gamemodes.FFA.GamemodeEntry();
            }
            else if(Config.GAMEMODE == "TDM")
            {
                currentGamemode = new Gamemodes.TDM.GamemodeEntry();
            }
            else
            {
                Console.WriteLine("[ERROR] Unknown gamemode " + Config.GAMEMODE);
                return;
            }
            SafeCall(currentGamemode!.OnStart);
        }
    }
}
