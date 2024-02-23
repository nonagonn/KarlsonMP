using Riptide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class Scoreboard
    {
        private static List<ScoreboardEntry> entries = new List<ScoreboardEntry>();
        public static void UpdateScoreboard(Message message)
        {
            entries.Clear();
            int count = message.GetInt();
            while(count-- > 0)
                entries.Add(new ScoreboardEntry(message.GetUShort(), message.GetString(), message.GetInt(), message.GetInt(), message.GetInt()));
            entries = entries.OrderByDescending(x => x.score).ToList();
        }

        private static Texture2D _grayTx, _blackTx;

        public static void GuiCtor()
        {
            _grayTx = new Texture2D(1, 1);
            _grayTx.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            _grayTx.Apply();
            _blackTx = new Texture2D(1, 1);
            _blackTx.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
            _blackTx.Apply();
        }

        private static bool scoreboardOpened = false;
        public static void Update()
        {
            scoreboardOpened = Input.GetKey(KeyCode.Tab);
        }

        public static void OnGUI()
        {
            if(!scoreboardOpened) return;
            // scoreboard header
            float basex = (Screen.width - 450f) / 2f, basey = 100f;
            GUI.DrawTexture(new Rect(basex, basey, 450f, 30f), _blackTx);
            GUI.Label(new Rect(basex + 5f, basey + 5f, 20f, 30f), "ID", MonoHooks.defaultLabel);
            GUI.Label(new Rect(basex + 50f, basey + 5f, 300f, 30f), "Username", MonoHooks.defaultLabel);
            GUI.Label(new Rect((Screen.width - 400f) / 2f + 300f, basey + 5f, 100f, 30f), "Kills", MonoHooks.defaultLabel);
            GUI.Label(new Rect((Screen.width - 400f) / 2f + 350f, basey + 5f, 100f, 30f), "Deaths", MonoHooks.defaultLabel);
            GUI.Label(new Rect((Screen.width - 400f) / 2f + 400f, basey + 5f, 100f, 30f), "Score", MonoHooks.defaultLabel);
            float yOff = 30f;
            bool alt = true;
            foreach(var score in entries)
            {
                if(alt)
                    GUI.DrawTexture(new Rect(basex, basey + yOff, 450f, 30f), _grayTx);
                else
                    GUI.DrawTexture(new Rect(basex, basey + yOff, 450f, 30f), _blackTx);
                alt = !alt;
                GUI.Label(new Rect(basex + 5f, basey + yOff + 5f, 20f, 30f), score.id.ToString(), MonoHooks.defaultLabel);
                GUI.Label(new Rect(basex + 50f, basey + yOff + 5f, 300f, 30f), score.name, MonoHooks.defaultLabel);
                GUI.Label(new Rect((Screen.width - 400f) / 2f + 300f, basey + yOff + 5f, 100f, 30f), score.kills.ToString(), MonoHooks.defaultLabel);
                GUI.Label(new Rect((Screen.width - 400f) / 2f + 350f, basey + yOff + 5f, 100f, 30f), score.deaths.ToString(), MonoHooks.defaultLabel);
                GUI.Label(new Rect((Screen.width - 400f) / 2f + 400f, basey + yOff + 5f, 100f, 30f), score.score.ToString(), MonoHooks.defaultLabel);
                yOff += 30f;
            }
        }

        public class ScoreboardEntry
        {
            public int id;
            public string name;
            public int kills;
            public int deaths;
            public int score;
            public ScoreboardEntry(int id, string name, int kills, int deaths, int score)
            {
                this.id = id;
                this.name = name;
                this.kills = kills;
                this.deaths = deaths;
                this.score = score;
            }
        }
    }
}
