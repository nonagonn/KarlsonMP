using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Preloader
{
    public class Entrypoint
    {
        public static Harmony harmony;

        // Preloader entrypoint
        public static void Start()
        {
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

            // todo: check for updates

            //window = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 200, 600, 400);
            window = new Rect(50, 50, 600, 400);
        }

        // After updater ran
        public static void PostUpdate()
        {
            UnityEngine.Object.Destroy(go); // destroy our monobehaviour
            // load kmp
            var kmp = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), "KMP", "KarlsonMP.dll"));
            kmp.GetType("KarlsonMP.Loader").GetMethod("Start").Invoke(null, Array.Empty<object>());
        }

        static bool fontLoaded = false;

        // OnGUI, before our dll is loaded
        static Rect window;
        public static void OnGUI()
        {
            if(!fontLoaded)
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(typeof(Entrypoint).Assembly.GetManifestResourceStream("Preloader.font"));
                Font font = bundle.LoadAsset<Font>("arial");
                GUI.skin.font = font;
                bundle.Unload(false);
                fontLoaded = true;
            }
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.height), grayTx, new Rect(0, 0, 1, 1));
            window = GUI.Window(1, window, (wid) =>
            {
                GUI.Label(new Rect(5, 20, 100, 20), "KarlsonMP preloader. nothing yet");
                GUI.Label(new Rect(5, 40, 100, 20), "will check for updates here");
                if (GUI.Button(new Rect(200, 350, 200, 30), "OK!")) PostUpdate();
                GUI.DragWindow();
            }, "KarlsonMP preloader");
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
