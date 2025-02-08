using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public static class ServerBrowser
    {
        static GameObject go;

        public static void Start()
        {
            go = new GameObject("ServerBrowser");
            go.AddComponent<ServerBrowserBehaviour>();
        }
        public static void Destroy()
        {
            UnityEngine.Object.Destroy(go);
        }
    }

    public class ServerBrowserBehaviour : MonoBehaviour
    {
        public void Start()
        {
            userName = PlayerPrefs.GetString("kmp_username", "");
            address = PlayerPrefs.GetString("kmp_address", "");
        }

        string userName, address;
        Rect window = new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100);

        public void OnGUI()
        {
            GUI.Box(window, "");
            GUI.Box(window, "");
            window = GUI.Window(1, window, _ =>
            {
                GUI.Label(new Rect(5, 20, 60, 20), "Username", MonoHooks.defaultLabel);
                GUI.Label(new Rect(5, 40, 60, 20), "IP:Port", MonoHooks.defaultLabel);
                userName = GUI.TextField(new Rect(60, 20, 145, 20), userName, MonoHooks.defaultTextArea);
                address = GUI.TextField(new Rect(60, 40, 145, 20), address, MonoHooks.defaultTextArea);
                if (GUI.Button(new Rect(5, 60, 190, 35), "Connect !", MonoHooks.defaultButton))
                {
                    // resolve address
                    int port = 11337;
                    if (address.Contains(':'))
                        port = int.Parse(address.Split(':')[1]);
                    string host = Loader.ToIPAddress(address.Split(':')[0]).ToString();
                    ServerBrowser.Destroy();
                    NetworkManager.Connect(host, port, userName);
                }
                GUI.DragWindow();
            }, "Server Browser [TBA]");
        }
    }
}
