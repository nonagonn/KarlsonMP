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
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, string lpString);

        public static string username;
        public static string address;
        public static int port;

        public static MonoHooks monoHooks;
        public static Harmony harmony;

        public static void Start()
        {
            if (Environment.GetCommandLineArgs().Length < 3)
            {
                MessageBox(IntPtr.Zero, "Invalid parameters", "KarlsonMP", 0);
                Process.GetCurrentProcess().Kill();
                return;
            }

            username = Environment.GetCommandLineArgs()[1];

            ForcedCultureInfo.Install();

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"KarlsonMP - {username}.log")))
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), $"KarlsonMP - {username}.log"));

            Application.logMessageReceived += (a, b, c) =>
            {
                if (c != LogType.Log)
                    KMP_Console.Log(a + " " + b);
            };

            string[] adr = Environment.GetCommandLineArgs()[2].Split(':');
            if (adr.Length == 1)
                port = 11337;
            else
                port = int.Parse(adr[1]);
            address = ToIPAddress(adr[0]).ToString();

            SetWindowText(FindWindow("", Application.productName), $"KarlsonMP - {address}:{port}");

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
                MessageBox(IntPtr.Zero, "Error while applying harmony patches!\nCheck KarlsonMP.log", "KarlsonMP", 0);
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
            KMP_Console.commands.Add("cmds", (args) =>
            {
                KMP_Console.Log("List of commands:", true);
                foreach (var c in KMP_Console.commands)
                    KMP_Console.Log(c.Key, true);
            });
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
