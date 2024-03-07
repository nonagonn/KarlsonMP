using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class MonoHooks : MonoBehaviour
    {
        public static GameObject playerPrefab;
        public static GameObject bulletPrefab;
        public static Texture2D _grayTx = new Texture2D(1, 1);
        public static GUIStyle _watermarkFont = new GUIStyle();
        public static GUIStyle defaultLabel, defaultButton, defaultWindow, defaultTextArea, defaultToggle, defaultToolbar;
        public static Font arialFont;

        public void Start()
        {
            // load prefabs
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Directory.GetCurrentDirectory(), "KMP", "karlsonmp.bundle"));
            playerPrefab = (GameObject)bundle.LoadAsset("assets/karlsonmp/playerprefab.prefab");
            Inventory.GuiCtor(bundle);
            Inventory.LoadAssets(bundle);
            KMP_AudioManager.Initialize(bundle);
            arialFont = bundle.LoadAsset<Font>("assets/karlsonmp/segoe-ui.ttf");
            KMP_Console.Log("monohooks start done");

            GuiCtor();
            PlaytimeLogic.Start();
            Scoreboard.GuiCtor();
            HUDMessages.GuiCtor();
        }

        public void FixedUpdate()
        {
            if (NetworkManager.client != null)
                NetworkManager.client.Update();

            PlaytimeLogic.FixedUpdate();
            KillFeedGUI._advance();
        }

        public void Update()
        {
            if(!Loader.OnLinux)
                Loader.discord.RunCallbacks();

            Time.timeScale = 1f;
            PlaytimeLogic.Update();
            Inventory.Update();
            Scoreboard.Update();
        }

        private static bool guiFontInit = false;
        public void OnGUI()
        {
            if(!guiFontInit)
            {
                KMP_Console.Log("initializing font components");
                guiFontInit = true;
                defaultLabel = new GUIStyle(GUI.skin.label) { font = arialFont, fontSize = 12 };
                defaultButton = new GUIStyle(GUI.skin.button) { font = arialFont, fontSize = 12 };
                defaultWindow = new GUIStyle(GUI.skin.window) { font = arialFont, fontSize = 12 };
                defaultTextArea = new GUIStyle(GUI.skin.textArea) { font = arialFont, fontSize = 12 };
                defaultToggle = new GUIStyle(GUI.skin.toggle) { font = arialFont, fontSize = 12 };
                defaultToolbar = new GUIStyle(GUI.skin.button) { font = arialFont, fontSize = 12 };
            }
            KillFeedGUI._draw();
            PlaytimeLogic.OnGUI();
            Inventory.OnGUI();
            Scoreboard.OnGUI();
            HUDMessages.OnGUI();

            if (dialogData.show)
            {
                dialogData.dialog_ = GUI.Window(71, dialogData.dialog_, (int id) =>
                {
                    GUI.Label(new Rect(5f, 20f, 496f, 150f), dialogData.content, MonoHooks.defaultLabel);
                    if (dialogData.yes == "")
                        dialogData.yes = "Yes";
                    bool btnYes = false, btnNo = false;
                    if (dialogData.no == "")
                    {
                        float w = Math.Max(MonoHooks.defaultButton.CalcSize(new GUIContent(dialogData.yes)).x, 90f);
                        btnYes = GUI.Button(new Rect((490f - w) / 2, 175f, w + 10f, 20f), dialogData.yes, MonoHooks.defaultButton);
                    }
                    else
                    {
                        float w = Math.Max(MonoHooks.defaultButton.CalcSize(new GUIContent(dialogData.yes)).x, 90f);
                        float w2 = Math.Max(MonoHooks.defaultButton.CalcSize(new GUIContent(dialogData.no)).x, 90f);
                        btnYes = GUI.Button(new Rect((240f - w) / 2, 175f, w + 10f, 20f), dialogData.yes, MonoHooks.defaultButton);
                        btnNo = GUI.Button(new Rect(250f + (240f - w2) / 2, 175f, w2 + 10f, 20f), dialogData.no, MonoHooks.defaultButton);
                    }
                    if (btnYes)
                    {
                        dialogData.onYes();
                        dialogData.show = false;
                    }
                    if (btnNo)
                    {
                        dialogData.onNo();
                        dialogData.show = false;
                    }
                    GUI.DragWindow();
                }, dialogData.title, MonoHooks.defaultWindow);
            }

            GUI.Label(new Rect(0f, 0f, Screen.width - 2, Screen.height - 2), "KarlsonMP reborn", _watermarkFont);
        }

        private void GuiCtor()
        {
            _grayTx.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            _grayTx.Apply();

            _watermarkFont.font = MonoHooks.arialFont;
            _watermarkFont.fontSize = 15;
            _watermarkFont.normal.textColor = Color.white;
            _watermarkFont.alignment = TextAnchor.LowerRight;
        }
        static class dialogData
        {
            public static string title;
            public static string content;
            public static string yes;
            public static string no;
            public static bool show;
            public static Rect dialog_ = new Rect((Screen.width - 500f) / 2, (Screen.height - 200f) / 2, 500f, 200f);
            public static Action onYes;
            public static Action onNo;
        };
        public static void ShowDialog(string _title, string _content, string _yes = "Yes", string _no = "No", Action onYes = null, Action onNo = null)
        {
            dialogData.title = _title;
            dialogData.content = _content;
            dialogData.yes = _yes;
            dialogData.no = _no;
            dialogData.show = true;
            dialogData.dialog_ = new Rect((Screen.width - 500f) / 2, (Screen.height - 200f) / 2, 500f, 200f);
            dialogData.onYes = onYes;
            dialogData.onNo = onNo;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnApplicationQuit()
        {
            NetworkManager.Quit();
            Process.GetCurrentProcess().Kill();
        }

        public void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                PlaytimeLogic.ForcePause(true);
            }
        }
    }
}
