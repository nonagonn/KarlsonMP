using ServerKMP.GamemodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerKMP
{
    internal class Program
    {
        

        static Thread mainThread;
        static bool serverRunning = true;
        public static bool IsServerRunning => serverRunning;
        public static void ExitServer() => serverRunning = false;

        static void Main(string[] _)
        {
            Config.LoadConfig();
            mainThread = new Thread(MainThread);
            mainThread.Start();

            CommandManager.Init();
            MapManager.Init();
            MapDownloader.Init();
            while (serverRunning)
            {
                string command = Console.ReadLine();
                string[] args = command.Split(' ');
                string label = args[0];
                CommandManager.Execute(label, args);
            }
        }

        static void MainThread()
        {
            Console.WriteLine($"Main Thread started, running at {Config.TPS} TPS (1 tick every {Config.MSPT} ms)");

            // start networking
            NetworkManager.Start();

            DateTime _nextLoop = DateTime.Now;
            while(serverRunning)
            {
                while(_nextLoop < DateTime.Now)
                {
                    Update();

                    // run scheduled tasks
                    foreach(var x in KMP_TaskScheduler.scheduledTasks)
                    {
                        if (x.Time < DateTime.Now)
                        {
                            x.Task();
                            x.ran = true;
                        }
                    }
                    KMP_TaskScheduler.scheduledTasks.RemoveAll(x => x.ran);

                    _nextLoop = _nextLoop.AddMilliseconds(Config.MSPT);
                    if(_nextLoop > DateTime.Now)
                        Thread.Sleep(_nextLoop - DateTime.Now); // don't overload server
                }
            }

            Console.WriteLine("Shutting down Main Thread");

            // shutdown server
            NetworkManager.Exit();
        }

        static void Update()
        {
            NetworkManager.Update();
        }
    }
}
