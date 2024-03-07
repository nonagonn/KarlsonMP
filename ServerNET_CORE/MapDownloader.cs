using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerKMP
{
    public static class MapDownloader
    {
        public static byte[] mapData = new byte[0];
        public static HttpListener? listener;
        public static Thread? HTTPserverThread;

        public static void Init()
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{Config.HTTP_PORT}/");
            listener.Start();
            HTTPserverThread = new Thread(() =>
            {
                while(Program.IsServerRunning)
                {
                    try
                    {
                        HttpListenerContext ctx = listener.GetContext();
                        Console.WriteLine("[MapDownloader] Request from " + ctx.Request.UserHostAddress);
                        HttpListenerResponse res = ctx.Response;
                        res.ContentLength64 = mapData.Length;
                        res.OutputStream.Write(mapData, 0, mapData.Length);
                        res.Close();
                    } catch { }
                }
            });
            HTTPserverThread.Start();
        }

        public static void Exit()
        {
            listener!.Stop();
            Console.WriteLine("[MapDownloader] Stopped.");
        }
    }
}
