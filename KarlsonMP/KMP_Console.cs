using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarlsonMP
{
    public static class KMP_Console
    {
        public static void Log(string message) => Log(message, false);
        public static void Log(string message, bool consoleOnly)
        {
            content += message + '\n';
            while (content.Split('\n').Length > 100) content = content.Substring(content.IndexOf("\n") + 1);
            if(!consoleOnly)
                File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), "KarlsonMP.log"), message + "\r\n");
        }

        public static string Content => content;
        private static string content = "KarlsonMP reborn\n    made by devilExE\n    licensed under MIT License\n    karlsonmodding/KarlsonMP @ github.com\n\n";

        public static Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();
        public static void _processCommand(string commandString)
        {
            string[] args = commandString.Split(' ');
            string label = args[0];
            if (!commands.ContainsKey(label))
                Log("<color=red>Unknwon command</color> " + label + ". Try running <color=yellow>cmds</color> for a list of commands.", true);
            else
                commands[label](args);
        }
    }
}
