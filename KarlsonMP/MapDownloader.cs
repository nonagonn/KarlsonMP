using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public static class MapDownloader
    {
        static WebClient wc = null;

        public static byte[] DownloadMap(string ipAddress, int port)
        {
            if(wc == null)
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                wc = new WebClient();
            }
            // add random garbage at the end to bypass any caching
            return wc.DownloadData($"http://{ipAddress}:{port}/{UnityEngine.Random.Range(0, 32768)}");
        }
    }
}
