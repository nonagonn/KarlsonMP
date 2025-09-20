using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace KarlsonMP
{
    public class Loader
    {
        public static MonoHooks monoHooks;
        public static Harmony harmony;
        public static string KMP_ROOT;

        public static void Start()
        {
            KMP_ROOT = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadson", "KarlsonMP");
            // here we are in Managers.Start already
            if (File.Exists(Path.Combine(KMP_ROOT, $"KarlsonMP.log")))
                File.Delete(Path.Combine(KMP_ROOT, $"KarlsonMP.log"));

            Application.logMessageReceived += (a, b, c) =>
            {
                if (c != LogType.Log)
                    KMP_Console.Log(a + " " + b);
            };
            
            GameObject mhooks = new GameObject("MonoHooks");
            monoHooks = mhooks.AddComponent<MonoHooks>();
            UnityEngine.Object.DontDestroyOnLoad(mhooks);

            // harmony
            harmony = new Harmony("karlsonmp");
            try
            {
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                KMP_Console.Log(e.ToString());
            }

            // register console commands
            KMP_Console.commands.Add("fpslimit", (args) =>
            {
                if (args.Length != 2) KMP_Console.Log("fpslimit [limit] - set fps limit", true);
                else if (!int.TryParse(args[1], out int val)) KMP_Console.Log("fpslimit [limit] - set fps limit", true);
                else
                {
                    Application.targetFrameRate = val;
                }
            });
            KMP_Console.commands.Add("fov", (args) =>
            {
                if (args.Length != 2) KMP_Console.Log("fov [value] - set fov", true);
                else if (!int.TryParse(args[1], out int val)) KMP_Console.Log("fov [value] - set fov", true);
                else
                {
                    GameState.Instance.SetFov(val);
                }
            });
            KMP_Console.commands.Add("clear", (args) =>
            {
                KMP_Console.ClearScreen();
            });
            KMP_Console.commands.Add("cc", (args) =>
            {
                PlaytimeLogic.ClearChat();
            });
            KMP_Console.commands.Add("cmds", (args) =>
            {
                KMP_Console.Log("List of commands:", true);
                foreach (var c in KMP_Console.commands)
                    KMP_Console.Log(c.Key, true);
            });

            Hook_Managers_Start.Run();
        }

        // https://stackoverflow.com/questions/12756302/resolving-an-ip-address-from-dns-in-c-sharp
        public static IPAddress ToIPAddress(string hostNameOrAddress, bool favorIpV6 = false)
        {
            var favoredFamily = favorIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            var addrs = Dns.GetHostAddresses(hostNameOrAddress);
            return addrs.FirstOrDefault(addr => addr.AddressFamily == favoredFamily)
                   ??
                   addrs.FirstOrDefault();
        }
    }
}
