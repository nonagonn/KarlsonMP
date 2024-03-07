using ServerNET_CORE;
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
                ServerStatus.SetServerStatus(3, "**Gamemode**  FFA");
            }
            else if(Config.GAMEMODE == "TDM")
            {
                currentGamemode = new Gamemodes.TDM.GamemodeEntry();
                ServerStatus.SetServerStatus(3, "**Gamemode**  TDM");
            }
            else
            {
                Console.WriteLine("[ERROR] Unknown gamemode " + Config.GAMEMODE);
            }
            SafeCall(currentGamemode!.OnStart);
        }
    }
}
