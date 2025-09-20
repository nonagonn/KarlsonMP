using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DevKit
{
    class Program
    {
        static readonly Dictionary<string, string> Download = new Dictionary<string, string> {
            { "RiptideNetworking.dll",  "https://github.com/RiptideNetworking/Riptide/releases/download/v2.0.0/RiptideNetworking.dll" },
            { "RiptideNetworking.xml",  "https://github.com/RiptideNetworking/Riptide/releases/download/v2.0.0/RiptideNetworking.xml" },
        };

        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "KarlsonMP reborn >> DevKit";
            Console.WriteLine("KarlsonMP reborn");
            Console.WriteLine("  made by devilexe");
            Console.WriteLine("  licensed under MIT license");
            Console.WriteLine("  karlsonmodding/KarlsonMP @ github.com");
            Console.WriteLine();
            Console.WriteLine(" >> DevKit");
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KarlsonMP reborn.sln")))
            {
                Console.WriteLine("Make sure you only run the devkit in the solution root.");
                Console.ReadKey();
                return;
            }

            if(Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib")))
            {
                Console.WriteLine("The 'lib' folder already exists.");
                Console.WriteLine("If you wish to remake the folder, please delete it");
                Console.ReadKey();
                return;
            }

            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib"));

            string gameRoot = "";
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                Console.WriteLine("Select Karlson.exe file from your game to get the game's root directory for assembly files");
                ofd.Title = "Select your Karlson instalation";
                ofd.Filter = "Karlson executable file|Karlson.exe";
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                DialogResult dr = ofd.ShowDialog();
                if (dr == DialogResult.OK)
                    gameRoot = Path.GetDirectoryName(ofd.FileName);
                else
                    return;
            }
            Console.WriteLine("Game root: " + gameRoot);

            Console.WriteLine("Copying game files..");
            foreach (string f in Directory.GetFiles(Path.Combine(gameRoot, "Karlson_Data", "Managed")))
            {
                if (!f.EndsWith(".dll")) continue;
                if (Path.GetFileName(f) == "Assembly-CSharp.dll" || Path.GetFileName(f) == "Unity.TextMeshPro.dll" || Path.GetFileName(f).StartsWith("UnityEngine"))
                {
                    File.Copy(f, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", Path.GetFileName(f)));
                    Console.WriteLine("Copied " + Path.GetFileName(f));
                }
            }

            Console.WriteLine("Downloading other assemblies..");
            HttpClient hc = new HttpClient();
            foreach(var d in Download)
            {
                Console.WriteLine("Downloading " + d.Key + " @ " + d.Value);
                File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", d.Key), hc.GetByteArrayAsync(d.Value).GetAwaiter().GetResult());
            }
            Console.WriteLine("DevKit installed succesfully the lib folder. Press any key to exit..");
            Console.ReadKey();
        }
    }
}
