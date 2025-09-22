using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Preloader
{
    public class Entrypoint
    {
        public const string API_ENDPOINT = "https://raw.githubusercontent.com/karlsonmodding/KarlsonMP/refs/heads/deploy";
        public static Harmony harmony;
        static string LOADSON_ROOT;

        static bool checking = true;
        static int filesDl = 0, filesToDl = 0;
        static WebClient wc;

        // Preloader entrypoint
        public static void Start()
        {
            LOADSON_ROOT = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadson");
            harmony = new Harmony("preloader");
            harmony.PatchAll();
        }

        // After game initialized
        static GameObject go;
        static Texture2D grayTx;
        public static void PostLoad()
        {
            ForcedCultureInfo.Install();
            grayTx = new Texture2D(1, 1);
            grayTx.SetPixel(0, 0, new Color(35f / 255f, 31f / 255f, 32f / 255f));
            grayTx.Apply();

            go = new GameObject("Preloader");
            go.AddComponent<PreloaderBehaviour>();

#if RELEASE
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
            wc = new WebClient();
            new Thread(() =>
            {
                var hashmap_raw = wc.DownloadString(API_ENDPOINT + "/hashmap");
                List<string> filesToUpdate = new List<string>();
                foreach (var hashinfo in hashmap_raw.Split('\n'))
                {
                    if (hashinfo.Length == 0) continue;
                    var file = hashinfo.Split(':')[0];
                    var hash = hashinfo.Split(':')[1];
                    if (!File.Exists(Path.Combine(LOADSON_ROOT, "KarlsonMP", file)) || CheckHash(Path.Combine(LOADSON_ROOT, "KarlsonMP", file)) != hash)
                        filesToUpdate.Add(file);
                }
                filesToDl = filesToUpdate.Count;
                checking = false;
                foreach (var file in filesToUpdate)
                {
                    File.WriteAllBytes(Path.Combine(LOADSON_ROOT, "KarlsonMP", file), wc.DownloadData(API_ENDPOINT + "/files/" + file));
                    ++filesDl;
                }
            }).Start();
#else
            PostUpdate();
#endif
        }

        static string CheckHash(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        // After updater ran
        static bool ran = false;
        public static void PostUpdate()
        {
            if (ran) return;
            ran = true;
#if RELEASE
            wc.Dispose();
#endif
            UnityEngine.Object.Destroy(go); // destroy our monobehaviour
            // load kmp
            var kmp = Assembly.LoadFrom(Path.Combine(LOADSON_ROOT, "KarlsonMP", "KarlsonMP.dll"));
            kmp.GetType("KarlsonMP.Loader").GetMethod("Start").Invoke(null, Array.Empty<object>());
        }

        // OnGUI, before our dll is loaded
        public static void OnGUI()
        {
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.width), grayTx, new Rect(0, 0, 1, 1));
            if (checking)
                GUI.Window(1, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 30, 300, 65), (wid) =>
                {
                    GUI.Label(new Rect(5, 20, 300, 20), "Please wait..");
                    GUI.Label(new Rect(5, 40, 300, 20), "Checking for updates");
                }, "KarlsonMP Updater");
            else if (filesDl == filesToDl)
                PostUpdate();
            else
                GUI.Window(1, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 30, 300, 65), (wid) =>
                {
                    GUI.Label(new Rect(5, 20, 300, 20), "Please wait, downloading files..");
                    GUI.Label(new Rect(5, 40, 300, 20), $"Progress: {filesDl} / {filesToDl}");
                }, "KarlsonMP Updater");
        }
    }

    public class PreloaderBehaviour : MonoBehaviour
    {
        public void OnGUI() => Entrypoint.OnGUI();
    }

    [HarmonyPatch(typeof(Managers), "Start")]
    public static class Managers_Start
    {
        public static bool Prefix(Managers __instance)
        {
            UnityEngine.Debug.Log("Detoured Managers.Start");
            UnityEngine.Object.DontDestroyOnLoad(__instance.gameObject);
            Time.timeScale = 1f;
            Application.targetFrameRate = 240;
            QualitySettings.vSyncCount = 0;

            AudioListener.volume = 0;
            AudioListener.pause = true;

            Entrypoint.PostLoad();
            return false;
        }
    }
}
