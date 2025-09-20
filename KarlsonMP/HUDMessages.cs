using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class HUDMessages
    {
        public static string topCenter;
        public static string aboveCrosshair;
        public static string subtitle;
        public static string bottomLeft;

        public static void ClearMessages() => topCenter = aboveCrosshair = subtitle = bottomLeft = "";

        private static GUIStyle _center, _lowerLeft;
        public static void GuiCtor()
        {
            _center = new GUIStyle();
            _center.normal.textColor = Color.white;
            _center.alignment = TextAnchor.UpperCenter;

            _lowerLeft = new GUIStyle();
            _lowerLeft.normal.textColor = Color.white;
            _lowerLeft.alignment = TextAnchor.LowerLeft;
        }

        public static void OnGUI()
        {
            if (topCenter != "")
                GUI.Label(new Rect(0f, 0f, Screen.width, 100f), topCenter, _center);
            if (aboveCrosshair != "")
                GUI.Label(new Rect(0f, Screen.height / 2f - 50f, Screen.width, 100f), aboveCrosshair, _center);
            if (subtitle != "")
                GUI.Label(new Rect(0f, Screen.height / 2f + 50f, Screen.width, 100f), subtitle, _center);
            if (bottomLeft != "")
                GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), bottomLeft, _lowerLeft);
        }
    }
}
