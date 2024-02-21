using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public static class KillFeedGUI
    {
        public static void AddText(string text)
        {
            feed.Add(text);
        }

        private static readonly List<_feedItem> feed = new List<_feedItem>();

        private static bool init = false;
        private static GUIStyle center;

        public static void _draw()
        {
            if (!init)
            {
                init = true;
                center = new GUIStyle();
                center.alignment = TextAnchor.MiddleCenter;
                center.normal.textColor = Color.white;
            }
            int offY = 0;
            foreach (var item in feed)
            {
                if (item.state < 25) SetGUIOpacity(item.state / 25f);
                else if (item.state > 125) SetGUIOpacity((150 - item.state) / 25f);
                else SetGUIOpacity(1f);
                GUI.Box(new Rect(Screen.width - 205, 5 + offY, 200, 40), "");
                GUI.Label(new Rect(Screen.width - 205, 5 + offY, 200, 40), item.text, center);
                offY += 45;
            }
            SetGUIOpacity(1f);
        }

        private static void SetGUIOpacity(float a) => GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, a);
        public static void _advance()
        {
            for (int i = 0; i < feed.Count; i++)
            {
                feed[i].state++;
                if (feed[i].state == 150)
                {
                    feed.RemoveAt(i);
                    i--;
                }
            }
        }

        private class _feedItem
        {
            public _feedItem(string _text)
            {
                text = _text;
                state = 0;
            }

            public string text;
            public int state;

            public static implicit operator _feedItem(string _text) { return new _feedItem(_text); }
        }
    }
}
