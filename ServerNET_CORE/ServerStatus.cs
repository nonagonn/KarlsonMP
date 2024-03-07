using ServerKMP;
using ServerKMP.GamemodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ServerNET_CORE
{
    public static class ServerStatus
    {
        /*
**IP**                    `redline2.go.ro`
**Players**          1/16
**Map**                kmpmap
**Gamemode**  FFA
         */
        public static string[] status = new string[] { "**IP**                    `redline2.go.ro`", "**Players**          0/16", "**Map**                1Sandbox0", "**Gamemode**  FFA" };
        static DateTime lastChannelUpdate = DateTime.Now;
        public static void SetServerStatus(ushort line, string msg)
        {
            if (!Config.ANNO) return;
            lastChannelUpdate = lastChannelUpdate.AddSeconds(30);
            if(lastChannelUpdate > DateTime.Now)
                Console.WriteLine("[ServerStatus] Changing Status: " + msg + " (in " + Math.Ceiling((lastChannelUpdate - DateTime.Now).TotalSeconds) + "s)");
            else
                Console.WriteLine("[ServerStatus] Changing Status: " + msg);
            status[line] = msg;
            KMP_TaskScheduler.Schedule(() =>
            {
                using (HttpClient hc = new HttpClient())
                using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, Config.DISCORD_API + "/status.php"))
                {
                    //req.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    req.Content = new StringContent("apikey=" + Config.API_KEY + "&status=" + HttpUtility.UrlEncode(string.Join("\\n", status)), Encoding.ASCII, "application/x-www-form-urlencoded");
                    var res = hc.Send(req);
                    var response = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Console.WriteLine("[ServerStatus] " + response);
                }
            }, lastChannelUpdate);
        }

        public static void MarkOffline()
        {
            if (!Config.ANNO) return;
            string msg = "**IP**                    `redline2.go.ro`\\n**Status**           Offline";
            lastChannelUpdate = lastChannelUpdate.AddSeconds(30);
            if (lastChannelUpdate > DateTime.Now)
            {
                Console.WriteLine("[ServerStatus] Waiting for rate limit to set offline message (" + Math.Ceiling((lastChannelUpdate - DateTime.Now).TotalSeconds) + "s)");
                Thread.Sleep(lastChannelUpdate - DateTime.Now); // wait for ratelimit
            }
            using (HttpClient hc = new HttpClient())
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, Config.DISCORD_API + "/status.php"))
            {
                //req.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                req.Content = new StringContent("apikey=" + Config.API_KEY + "&status=" + HttpUtility.UrlEncode(msg), Encoding.ASCII, "application/x-www-form-urlencoded");
                var res = hc.Send(req);
                var response = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine("[ServerStatus] " + response);
            }
        }
    }
}
