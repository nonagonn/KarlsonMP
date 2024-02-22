using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class GuiWindow
    {
        private static int widc = 0;

        public GuiWindow(string _title, int x, int y, int width, int height, Action _content, object _storage = null, bool _show = false)
        {
            title = _title;
            rect = new Rect(x, y, width, height);
            content = _content;
            storage = _storage;
            show = _show;
            wid = widc++;
        }

        public object storage;

        private int wid;
        private string title;
        private Rect rect;
        private Action content;

        public void draw()
        {
            if (!show) return;
            rect = GUI.Window(wid, rect, (_) => {
                content();
                GUI.DragWindow();
            }, title);
        }
        public bool show;
    }

    class PlaytimeLogic
    {
        public static bool paused = false;
        public static int hp = 100;

        public static void ForcePause(bool toggle)
        {
            paused = toggle;
            pauseTick = 0;
            pauseState = 0;
            if (!toggle)
                chatOpened = false;
        }

        public static void Update()
        {
            if (NetworkManager.client == null || !NetworkManager.client.IsConnected)
                return;

            if (Input.GetButtonDown("Cancel"))
            {
                if (chatOpened)
                    chatOpened = false;
                else
                    ForcePause(!paused);
            }
            if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.Return))
                chatOpened = true;

            if (paused)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (pauseState < watermark.Length + 5)
                {
                    pauseTick++;
                    if (pauseTick == 10)
                    {
                        pauseState++;
                        pauseTick = 0;
                    }
                }
                if (Input.GetKeyDown(KeyCode.BackQuote))
                    console.show = !console.show;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public static void FixedUpdate()
        {
            if (NetworkManager.client == null || !NetworkManager.client.IsConnected)
                return;
            ClientSend.PositionData();
        }

        private static int pauseState = 0, pauseTick = 0;
        private const string watermark = "made by devilexe.\nmodels by nonagon.";
        public static bool showDebug = true;
        private static string ConsoleInput = "";

        private static bool chatOpened = false;
        private static string chatContent = "";
        private static string chat = "Press <b>Y</b> or <b>Enter</b> to chat.";
        public static void AddChat(string msg)
        {
            chat += "\n" + msg;
            while(chat.Split('\n').Length > 15)
                chat = chat.Substring(chat.IndexOf("\n") + 1);
        }

        private static Texture2D hpBar_black, hpBar_green;
        private static GUIStyle nameStyle;
        public static void Start()
        {
            hpBar_black = new Texture2D(1, 1);
            hpBar_black.SetPixel(0, 0, Color.black);
            hpBar_black.Apply();
            hpBar_green = new Texture2D(1, 1);
            hpBar_green.SetPixel(0, 0, new Color(0f, 0.5f, 0f));
            hpBar_green.Apply();
            nameStyle = new GUIStyle();
            nameStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

            console = new GuiWindow("console", 50, 50, 800, 500, () =>
            {
                if (GUI.Button(new Rect(750, 0, 50, 20), "Close")) console.show = false;
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(KMP_Console.Content));
                console.storage = GUI.BeginScrollView(new Rect(5, 20, 785, 460), (Vector2)console.storage, new Rect(0, 0, size.x, size.y));
                GUI.Label(new Rect(0, 0, size.x, size.y), KMP_Console.Content);
                GUI.EndScrollView();
                GUI.SetNextControlName("ConsoleInput");
                ConsoleInput = GUI.TextArea(new Rect(0, 480, 800, 20), ConsoleInput);
                GUI.FocusControl("ConsoleInput");
                if(ConsoleInput.Contains("\n"))
                {
                    KMP_Console._processCommand(ConsoleInput.Replace("\n", ""));
                    ConsoleInput = "";
                }
                if(ConsoleInput.Contains("`"))
                {
                    ConsoleInput = ConsoleInput.Replace("`", "");
                    console.show = !console.show;
                }
            }, new Vector2(0, 0));

            settings = new GuiWindow("settings", Screen.width / 2 - 220, Screen.height / 2 - 102, 440, 205, () =>
            {
                if (GUI.Button(new Rect(390, 0, 50, 20), "Close")) settings.show = false;
                GUI.Label(new Rect(5f, 25f, 100f, 20f), "<b>Graphics</b>");
                GameState.Instance.SetGraphics(GUI.Toolbar(new Rect(5f, 45f, 100f, 20f), GameState.Instance.GetGraphics() ? 0 : 1, new string[] { "Good", "Fast" }) == 0);

                GUI.Label(new Rect(5f, 70f, 100f, 20f), "<b>Motion Blur</b>");
                GameState.Instance.SetBlur(GUI.Toolbar(new Rect(5f, 90f, 100f, 20f), GameState.Instance.blur ? 0 : 1, new string[] { "On", "Off" }) == 0);

                GUI.Label(new Rect(5f, 115f, 100f, 20f), "<b>Cam Shake</b>");
                GameState.Instance.SetShake(GUI.Toolbar(new Rect(5f, 135f, 100f, 20f), GameState.Instance.shake ? 0 : 1, new string[] { "On", "Off" }) == 0);

                GUI.Label(new Rect(5f, 160f, 100f, 20f), "<b>Slow-mo</b>");
                GUI.Label(new Rect(5f, 180f, 100f, 20f), "Off by KMP");

                GUI.Label(new Rect(120f, 25f, 150f, 20f), "<b>Sensitivity</b>");
                GameState.Instance.SetSensitivity(GUI.HorizontalSlider(new Rect(120f, 45f, 120f, 20f), GameState.Instance.GetSensitivity(), 0.1f, 3.0f));
                GameState.Instance.SetSensitivity(float.Parse(GUI.TextField(new Rect(240f, 45f, 30f, 20f), GameState.Instance.GetSensitivity().ToString("0.00"))));

                GUI.Label(new Rect(120f, 70f, 150f, 20f), "<b>Volume</b>");
                GameState.Instance.SetVolume(GUI.HorizontalSlider(new Rect(120f, 90f, 120f, 20f), GameState.Instance.GetVolume(), 0f, 1f));
                GameState.Instance.SetVolume(float.Parse(GUI.TextField(new Rect(240f, 90f, 30f, 20f), GameState.Instance.GetVolume().ToString("0.00"))));

                GUI.Label(new Rect(120f, 115f, 150f, 20f), "<b>Music</b>");
                GameState.Instance.SetMusic(GUI.HorizontalSlider(new Rect(120f, 135f, 120f, 20f), GameState.Instance.GetMusic(), 0f, 1f));
                GameState.Instance.SetMusic(float.Parse(GUI.TextField(new Rect(240f, 135f, 30f, 20f), GameState.Instance.GetMusic().ToString("0.00"))));

                GUI.Label(new Rect(120f, 160f, 150f, 20f), "<b>FOV</b>");
                GameState.Instance.SetFov(Mathf.Floor(GUI.HorizontalSlider(new Rect(120f, 180f, 120f, 20f), GameState.Instance.GetFov(), 50f, 150f)));
                GameState.Instance.SetFov(Mathf.Floor(float.Parse(GUI.TextField(new Rect(240f, 180f, 30f, 20f), GameState.Instance.GetFov().ToString("0")))));

                var fpsOn = typeof(Debug).GetField("fpsOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var speedOn = typeof(Debug).GetField("speedOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var ins = UnityEngine.Object.FindObjectOfType<Debug>();
                fpsOn.SetValue(ins, GUI.Toggle(new Rect(285f, 25f, 100f, 20f), (bool)fpsOn.GetValue(ins), "Show FPS"));
                speedOn.SetValue(ins, GUI.Toggle(new Rect(285f, 45f, 100f, 20f), (bool)speedOn.GetValue(ins), "Show Speed"));
                showDebug = GUI.Toggle(new Rect(285f, 65f, 130f, 20f), showDebug, "Show RTT (ping)");
            });
        }

        static GuiWindow settings, console;

        public static void OnGUI()
        {
            if (!ClientHandle.PlayerList) return;
            // hp
            GUI.Label(new Rect(50f, Screen.height - 100f, 150f, 50f), $"<size=50><color=green><b>+</b></color> {hp}</size>");
            GUI.DrawTexture(new Rect(50f, Screen.height - 45f, 128f, 20f), hpBar_black);
            GUI.DrawTexture(new Rect(50f, Screen.height - 45f, 128f * hp / 100f, 20f), hpBar_green);

            // draw names
            foreach (Player p in players)
            {
                string text = "(" + p.id + ") " + p.username;

                Vector3 pos = Camera.main.WorldToScreenPoint(p.player.transform.position + new Vector3(0f, 2f, 0f));
                if (Vector3.Distance(p.player.transform.position, PlayerMovement.Instance.transform.position) >= 150f)
                    continue; // player is too far
                if (pos.z < 0)
                    continue; // point is behind our camera
                Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(text));
                textSize.x += 10f;
                GUI.Label(new Rect(pos.x - textSize.x / 2, (Screen.height - pos.y) - textSize.y / 2, textSize.x, textSize.y), text, nameStyle);
            }

            if (showDebug)
                GUI.Label(new Rect(Screen.width - 121f, Screen.height - 35f, 100f, 20f), $"<color=white>RTT: {NetworkManager.client.SmoothRTT}</color>");

            // chat
            GUI.Label(new Rect(0f, 20f, 500f, 240f), chat);
            if(chatOpened)
            {
                GUI.SetNextControlName("chatcontrol");
                chatContent = GUI.TextArea(new Rect(0f, 250f, 500f, 20f), chatContent);
                GUI.FocusControl("chatcontrol");
                if(chatContent.Contains('\n'))
                {
                    chatContent.Replace("\n", "");
                    if(chatContent.Trim().Length > 0)
                        ClientSend.ChatMessage(chatContent.Trim());
                    chatContent = "";
                    chatOpened = false;
                }
                if(chatContent.Contains('`'))
                {
                    chatContent = "";
                    chatOpened = false;
                }
            }

            // pause screen
            if (!paused)
                return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), MonoHooks._grayTx);
            if (pauseState > 0) if (GUI.Button(new Rect(50, Screen.height - 70, 120, 20), "Exit Game")) Application.Quit();
            if (pauseState > 1) if (GUI.Button(new Rect(50, Screen.height - 90, 120, 20), "Return to game")) paused = false;
            if (pauseState > 2) if (GUI.Button(new Rect(50, Screen.height - 110, 120, 20), "Settings")) settings.show = !settings.show;
            if (pauseState > 3) GUI.Button(new Rect(50, Screen.height - 130, 120, 20), "Player List");
            if (pauseState > 4) if (GUI.Button(new Rect(50, Screen.height - 150, 120, 20), "Console")) console.show = !console.show;
            if (pauseState > 5) GUI.Label(new Rect(50, Screen.height - 185, 120, 40), watermark.Substring(0, pauseState - 5));

            console.draw();
            settings.draw();
        }
        public static List<Player> players = new List<Player>();
    }

    public class KeyboardState
    {
        public float Horizontal;
        public float Vertical;
        public bool Jump;
        public bool Crouch;
        public bool Pickup;
        public bool Drop;
        public bool Fire1;
        public float rotX;
        public float rotY;
        public void Reset()
        {
            Horizontal = 0;
            Vertical = 0;
            Jump = false;
            Crouch = false;
            Pickup = false;
            Drop = false;
            Fire1 = false;
        }
        public void SyncWith(KeyboardState other)
        {
            Horizontal = other.Horizontal;
            Vertical = other.Vertical;
            Jump = other.Jump;
            Crouch = other.Crouch;
            Pickup = other.Pickup;
            Drop = other.Drop;
            Fire1 = other.Fire1;
            rotX = other.rotX;
            rotY = other.rotY;
        }
        public KeyboardState Clone()
        {
            KeyboardState ks = new KeyboardState();
            ks.SyncWith(this);
            return ks;
        }

        // KMP Server-side code, for client-side prediction
        public bool oldCrouch = false;
        public bool StartCrouch()
        {
            return !oldCrouch && Crouch;
        }
        public bool StopCrouch()
        {
            return oldCrouch && !Crouch;
        }
        public void SyncCrouch()
        {
            oldCrouch = Crouch;
        }
    }
}
