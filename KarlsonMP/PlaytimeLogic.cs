using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KarlsonMP
{
    class PlaytimeLogic
    {
        public static bool paused = false;
        public static int hp = 100;
        public static int spectatingId { get; private set; } = 0;
        public static bool showNametags = true;
        public static bool suicided = false;
        public static bool enableCollisions = true;
        public static bool InLevel = false;
        public static void StartSpectate(int targetId)
        {
            spectatingId = targetId;
            GameObject.Find("Camera/Main Camera/GunCam").SetActive(false);
        }
        public static void ExitSpectate()
        {
            spectatingId = 0;
            GameObject.Find("Camera/Main Camera/GunCam").SetActive(true);
        }

        public static void ForcePause(bool toggle)
        {
            paused = toggle;
            pauseTick = 0;
            pauseState = 0;
            chatOpened = false;
        }

        static bool NetStatsOpen = false;
        static float SendRate, SendRateMsg, RecvRate, RecvRateMsg, NetStatsTimeSinceLastUpdate = 0;

        public static void Update()
        {
            if (NetworkManager.client == null || !NetworkManager.client.IsConnected)
                return;

            if (Input.GetKeyDown(KeyCode.F5))
                NetStatsOpen = !NetStatsOpen;
            if(NetStatsOpen)
            {
                NetStatsTimeSinceLastUpdate += Time.deltaTime;
                if (NetStatsTimeSinceLastUpdate >= 0.5f)
                {
                    (SendRate, SendRateMsg) = NetworkManager.client.Connection.Metrics.SendRate();
                    SendRate /= NetStatsTimeSinceLastUpdate;
                    SendRateMsg /= NetStatsTimeSinceLastUpdate;
                    (RecvRate, RecvRateMsg) = NetworkManager.client.Connection.Metrics.RecvRate();
                    RecvRate /= NetStatsTimeSinceLastUpdate;
                    RecvRateMsg /= NetStatsTimeSinceLastUpdate;
                    NetStatsTimeSinceLastUpdate = 0;
                }
            }

            if (spectatingId != 0)
            {
                float num = Input.GetAxis("Mouse X") * GameState.Instance.GetSensitivity() * 50f * Time.fixedDeltaTime;
                float num2 = Input.GetAxis("Mouse Y") * GameState.Instance.GetSensitivity() * 50f * Time.fixedDeltaTime;
                var playerCam = PlayerMovement.Instance.playerCam;
                float desiredX = playerCam.transform.localRotation.eulerAngles.y + num;
                float xRotation = playerCam.transform.localRotation.eulerAngles.x - num2;
                if (xRotation > 180f) xRotation -= 360f;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);
                playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0f);
                if(spectatingId != NetworkManager.client.Id)
                {
                    var player = (from x in PlaytimeLogic.players where x.id == spectatingId select x).First();
                    PlayerMovement.Instance.transform.position = player.player.transform.position - playerCam.transform.forward * 10f;
                }
                else
                {
                    PlayerMovement.Instance.rb.velocity = Vector3.zero;
                }
            }

            if (Input.GetButtonDown("Cancel"))
            {
                if (chatOpened)
                    chatOpened = false;
                else
                    ForcePause(!paused);
            }
            if(!paused)
            {
                if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.Return))
                    chatOpened = true;
                if(Input.GetKeyDown(KeyCode.P))
                {
                    PasswordDialog.Prompt("", Array.Empty<byte>());
                }
            }

            if (paused)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
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

            // process download queue
            FileHandler.ProcessDownloadQueue();

            if (paused)
            {
                if (pauseState < watermark.Length + 5)
                {
                    pauseTick++;
                    if (pauseTick == 2)
                    {
                        pauseState++;
                        pauseTick = 0;
                    }
                }
            }

            ClientSend.PositionData();
        }

        public static void PrepareMapChange()
        {
            // clear old player list
            players.Clear();
            ClientHandle.ResetPlayerList();
            // delete old props
            PropManager.ClearProps();
            BulletRenderer.DeleteBullets();
        }

        public static class PasswordDialog
        {
            public static bool Opened = false;
            public static string Caption;
            public static byte[] CspBlob;
            public static string Input;
            public static void Prompt(string prompt, byte[] cspBlob)
            {
                if (prompt == "")
                    prompt = "Server prompted you for a password.";
                Caption = prompt;
                CspBlob = cspBlob;
                Opened = true;
                Input = "";
                ForcePause(true);
            }
            public static void SendPassword()
            {
                Opened = false;
                if (Input.Length == 0)
                {
                    ClientSend.Password(Array.Empty<byte>());
                    return;
                }
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.ImportCspBlob(CspBlob);
                    byte[] pwEnc = rsa.Encrypt(Encoding.UTF8.GetBytes(Input), false);
                    ClientSend.Password(pwEnc);
                    ForcePause(false);
                }
            }
        }

        private static int pauseState = 0, pauseTick = 0;
        private const string watermark = "made by devilexe.\nmodels by nonagon.";
        public static bool showDebug = true;
        private static string ConsoleInput = "";

        private static bool chatOpened = false;
        private static string chatContent = "";
        private static string chat = "Press <b>Y</b> or <b>Enter</b> to chat.";
        static string chat_stripped = "Press <b>Y</b> or <b>Enter</b> to chat.";
        public static void AddChat(string msg)
        {
            chat += "\n" + msg;
            while(chat.Split('\n').Length > 15)
                chat = chat.Substring(chat.IndexOf("\n") + 1);
            chat_stripped = StripColor(chat);
        }
        public static void ClearChat() => chat = chat_stripped = "";

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
                if(!(bool)settings.storage && GameState.Instance != null)
                {
                    settings_graphics = new GuiSwitch(GameState.Instance.SetGraphics, GameState.Instance.GetGraphics(), new Rect(5f, 45f, 100f, 20f), "Good", "Fast");
                    settings_motion_blur = new GuiSwitch(GameState.Instance.SetBlur, GameState.Instance.blur, new Rect(5f, 90f, 100f, 20f), "On", "Off");
                    settings_cam_shake = new GuiSwitch(GameState.Instance.SetShake, GameState.Instance.shake, new Rect(5f, 135f, 100f, 20f), "On", "Off");
                    settings_sensitivity = new GuiSliderAndTextbox(GameState.Instance.SetSensitivity, GameState.Instance.GetSensitivity(), 0.1f, 3.0f, new Rect(120f, 45f, 120f, 20f), new Rect(240f, 45f, 30f, 20f));
                    settings_volume = new GuiSliderAndTextbox(GameState.Instance.SetVolume, GameState.Instance.GetVolume(), 0f, 1f, new Rect(120f, 90f, 120f, 20f), new Rect(240f, 90f, 30f, 20f));
                    settings_music = new GuiSliderAndTextbox(GameState.Instance.SetMusic, GameState.Instance.GetMusic(), 0f, 1f, new Rect(120f, 135f, 120f, 20f), new Rect(240f, 135f, 30f, 20f));
                    settings_fov = new GuiSliderAndTextbox(GameState.Instance.SetFov, GameState.Instance.GetFov(), 50f, 150f, new Rect(120f, 180f, 120f, 20f), new Rect(240f, 180f, 30f, 20f));
                    var debugInstance = UnityEngine.Object.FindObjectOfType<Debug>();
                    settings_fps_on = new GuiReflectionCheckbox(typeof(Debug).GetField("fpsOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), debugInstance, new Rect(285f, 25f, 100f, 20f), "Show FPS");
                    settings_speed_on = new GuiReflectionCheckbox(typeof(Debug).GetField("speedOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), debugInstance, new Rect(285f, 45f, 100f, 20f), "Show Speed");

                    settings.storage = true;
                }
                if (!(bool)settings.storage)
                    return;

                if (GUI.Button(new Rect(390, 0, 50, 20), "Close")) settings.show = false;
                GUI.Label(new Rect(5f, 25f, 100f, 20f), "<b>Graphics</b>");
                settings_graphics.draw();

                GUI.Label(new Rect(5f, 70f, 100f, 20f), "<b>Motion Blur</b>");
                settings_motion_blur.draw();

                GUI.Label(new Rect(5f, 115f, 100f, 20f), "<b>Cam Shake</b>");
                settings_cam_shake.draw();

                GUI.Label(new Rect(5f, 160f, 100f, 20f), "<b>Slow-mo</b>");
                GUI.Label(new Rect(5f, 180f, 100f, 20f), "Off by KMP");

                GUI.Label(new Rect(120f, 25f, 150f, 20f), "<b>Sensitivity</b>");
                settings_sensitivity.draw();

                GUI.Label(new Rect(120f, 70f, 150f, 20f), "<b>Volume</b>");
                settings_volume.draw();

                GUI.Label(new Rect(120f, 115f, 150f, 20f), "<b>Music</b>");
                settings_music.draw();

                GUI.Label(new Rect(120f, 160f, 150f, 20f), "<b>FOV</b>");
                settings_fov.draw();

                settings_fps_on.draw();
                settings_speed_on.draw();
                showDebug = GUI.Toggle(new Rect(285f, 65f, 130f, 20f), showDebug, "Show RTT (ping)");
            }, false);

            password = new GuiWindow("Enter Password (E2E encrypted)", Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150, () =>
            {
                GUI.Label(new Rect(5f, 25f, 300, 60), PasswordDialog.Caption);
                GUI.SetNextControlName("passfield");
                PasswordDialog.Input = GUI.PasswordField(new Rect(10f, 120f, 280f, 20f), PasswordDialog.Input, '●');
                GUI.FocusControl("passfield");
                if (Event.current.type == EventType.KeyDown && Event.current.character == '\n')
                    PasswordDialog.SendPassword();
            }, null, _show: true);
        }

        static GuiWindow settings, console, password;
        static GuiSwitch settings_graphics, settings_motion_blur, settings_cam_shake;
        static GuiSliderAndTextbox settings_sensitivity, settings_volume, settings_music, settings_fov;
        static GuiReflectionCheckbox settings_fps_on, settings_speed_on;

        static string StripColor(string s)
        {
            while(s.Contains("<color"))
            {
                int idx = s.IndexOf("<color");
                int close = s.IndexOf('>', idx);
                s = s.Substring(0, idx) + s.Substring(close + 1);
            }
            return s.Replace("</color>", "");
        }

        static readonly (int, int)[] chatOutline = new (int, int)[] { (-2, -1), (-2, 0), (-2, 1), (-1, -2), (-1, -1), (-1, 0), (-1, 1), (-1, 2), (0, -2), (0, -1), (0, 1), (0, 2), (1, -2), (1, -1), (1, 0), (1, 1), (1, 2), (2, -1), (2, 0), (2, 1) };

        public static void OnGUI()
        {
            if (PasswordDialog.Opened)
                password.draw();
            if (!ClientHandle.PlayerList) return;
            // hp
            if(spectatingId == 0 && hp > 0)
            {
                GUI.Label(new Rect(50f, Screen.height - 110f, 150f, 70f), $"<size=50><color=green><b>+</b></color> {hp}</size>");
                GUI.DrawTexture(new Rect(50f, Screen.height - 45f, 135f, 20f), hpBar_black);
                GUI.DrawTexture(new Rect(50f, Screen.height - 45f, 135f * hp / 100f, 20f), hpBar_green);
            }

            // draw names
            foreach (Player p in players)
            {
                if (!p.nametagShown) continue;
                string text = "(" + p.id + ") " + p.username;

                var dist = Vector3.Distance(p.player.transform.position, PlayerMovement.Instance.transform.position);
                Vector3 pos = Camera.main.WorldToScreenPoint(p.player.transform.position + new Vector3(0f, 0.5f + dist * 0.01f, 0f));
                // TODO: make nametag distance variable server-controller
                if (dist >= 50f)
                    continue; // player is too far
                if (pos.z < 0)
                    continue; // point is behind our camera
                // TODO: check if we have line of sight to the player maybe?
                Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(text));
                textSize.x += 10f;
                GUI.Label(new Rect(pos.x - textSize.x / 2, (Screen.height - pos.y) - textSize.y / 2, textSize.x, textSize.y), text, nameStyle);
            }
            
            if (showDebug)
                GUI.Label(new Rect(Screen.width - 78f, Screen.height - 35f, 100f, 20f), $"<color=white>RTT: {NetworkManager.client.SmoothRTT}</color>");

            if(NetStatsOpen)
            {
                GUI.Label(new Rect(5f, 25f, Screen.width, Screen.height), $"<size=25>[Network Statistics]\n" +
                    $"Received messages: {NetworkManager.client.Connection.Metrics.MessagesIn} ({NetworkManager.client.Connection.Metrics.BytesIn} bytes)\n" +
                    $"Sent messages: {NetworkManager.client.Connection.Metrics.MessagesOut} ({NetworkManager.client.Connection.Metrics.BytesOut} bytes)\n" +
                    $"Send rate: {SendRate / 1000:0.00}kB/s ({SendRateMsg:0.00}msg/s)\n" +
                    $"Recv rate: {RecvRate / 1000:0.00}kB/s ({RecvRateMsg:0.00}msg/s)\n" +
                    $"RTT: {NetworkManager.client.RTT}ms (avg {NetworkManager.client.SmoothRTT}ms)</size>");
            }
            else
            {
                // chat
                var sz = GUI.skin.label.CalcSize(new GUIContent(chat));
                foreach((int i, int j) in chatOutline)
                    GUI.Label(new Rect(5 + i, 25 + j, sz.x + 10, sz.y + 10), "<color=black>" + chat_stripped + "</color>");
                GUI.Label(new Rect(5f, 25f, sz.x + 10, sz.y + 10), chat);
                if (chatOpened)
                {
                    GUI.SetNextControlName("chatcontrol");
                    chatContent = GUI.TextArea(new Rect(0f, Math.Max(30f + sz.y, 265f), 500f, 20f), chatContent);
                    GUI.FocusControl("chatcontrol");
                    if (chatContent.Contains('\n'))
                    {
                        chatContent.Replace("\n", "");
                        if (chatContent.Trim().Length > 0)
                            ClientSend.ChatMessage(chatContent.Trim());
                        chatContent = "";
                        chatOpened = false;
                    }
                    if (chatContent.Contains('`'))
                    {
                        chatContent = "";
                        chatOpened = false;
                    }
                }
            }

            // pause screen
            if (!paused)
                return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), MonoHooks._grayTx);
            if (pauseState > 0) if (GUI.Button(new Rect(50, Screen.height - 70, 120, 20), "Exit Game")) Application.Quit();
            if (pauseState > 1) if (GUI.Button(new Rect(50, Screen.height - 90, 120, 20), "Return to game")) paused = false;
            if (pauseState > 2) if (GUI.Button(new Rect(50, Screen.height - 110, 120, 20), "Disconnect")) DisconnectToBrowser();
            if (pauseState > 3) if (GUI.Button(new Rect(50, Screen.height - 130, 120, 20), "Settings")) settings.show = !settings.show;
            if (pauseState > 4) if (GUI.Button(new Rect(50, Screen.height - 150, 120, 20), "Console")) console.show = !console.show;
            if (pauseState > 5) GUI.Label(new Rect(50, Screen.height - 185, 120, 40), watermark.Substring(0, pauseState - 5));

            console.draw();
            settings.draw();
        }
        public static List<Player> players = new List<Player>();

        public static void DisconnectToBrowser()
        {
            NetworkManager.Quit();
            paused = false;
            NetworkManager.client = null;
            ClientHandle.ResetPlayerList();
            players.Clear();
            PropManager.ClearProps();
            BulletRenderer.DeleteBullets();
            Inventory.ReloadAll();
            chat = chat_stripped = "Press <b>Y</b> or <b>Enter</b> to chat.";
            HUDMessages.ClearMessages();
            Inventory.selfBulletColor = Color.blue;
            Physics.IgnoreLayerCollision(8, 8, false);
            Physics.IgnoreLayerCollision(8, 12, false);
            InLevel = false;
            Game.Instance.MainMenu();
        }
    }
}
