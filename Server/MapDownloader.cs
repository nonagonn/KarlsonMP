using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerKMP
{
    public static class MapDownloader
    {
        public static byte[] mapData = new byte[0];
        public static HttpListener listener;
        public static Thread HTTPserverThread;

        public static void Init()
        {
            listener = new HttpListener();
            try
            {
                listener.Prefixes.Add($"http://+:{Config.HTTP_PORT}/");
                listener.Start();
            }
            catch
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/C netsh http add urlacl url=\"http://+:{Config.HTTP_PORT}/\" user=\"{System.Security.Principal.WindowsIdentity.GetCurrent().Name}\"";
                startInfo.Verb = "runas";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                listener = new HttpListener();
                listener.Prefixes.Add($"http://+:{Config.HTTP_PORT}/");
                listener.Start();
            }
            HTTPserverThread = new Thread(() =>
            {
                while(true)
                {
                    HttpListenerContext ctx = listener.GetContext();
                    Console.WriteLine("[MapDownloader] Request from " + ctx.Request.UserHostAddress);
                    HttpListenerResponse res = ctx.Response;
                    res.ContentLength64 = mapData.Length;
                    res.OutputStream.Write(mapData, 0, mapData.Length);
                    res.Close();
                }
            });
            HTTPserverThread.Start();
        }

        public static void Exit()
        {
            HTTPserverThread.Abort();
            listener.Stop();
        }
    }
}
