using Riptide;
using Riptide.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KarlsonMP
{
    public class NetworkManager
    {
        public static Client client;
        public static void Connect()
        {
            RiptideLogger.Initialize(KMP_Console.Log, false);
            client = new Client("Riptide"); // logger name
            client.Connect(Loader.address + ":" + Loader.port);
            client.Connected += DidConnect;
            client.ConnectionFailed += FailedToConnect;
            client.Disconnected += DidDisconnect;
        }

        public static void Quit()
        {
            if(!client.IsNotConnected)
                client.Disconnect();
        }

        private static void DidConnect(object sender, EventArgs e)
        {
            ClientSend.Handshake();
        }

        private static void FailedToConnect(object sender, ConnectionFailedEventArgs e)
        {
            MonoHooks.ShowDialog("KarlsonMP reborn", "Failed to connect to the server", "Exit game", "", () => { Application.Quit(); });
        }

        private static void DidDisconnect(object sender, DisconnectedEventArgs e)
        {
            MonoHooks.ShowDialog("KarlsonMP reborn", "Server closed the connection", "Exit game", "", () => { Application.Quit(); });
        }
    }

    public static class Packet_S2C
    {
        public const ushort initialPlayerList = 1; // initial player list
        public const ushort addPlayer = 2; // player join/left
        public const ushort playerData = 3; // server snapshot of the world
        public const ushort bullet = 4; // bullet ray
        public const ushort killFeed = 5;
        public const ushort teleport = 6;
        public const ushort map = 7;
    }
    public static class Packet_C2S
    {
        public const ushort handshake = 1; // handshake, send username
        public const ushort position = 2; // position
        public const ushort requestScene = 3; // request sync and initialPlayerList
        public const ushort shoot = 4; // client shoot
        public const ushort kill = 5; // client killed someone
    }

    public class ClientSend
    {
        public static void Handshake()
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_C2S.handshake);
            message.Add(Loader.username);
            NetworkManager.client.Send(message);
        }

        public static void PositionData()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, Packet_C2S.position);
            message
                .Add(PlayerMovement.Instance.transform.position)
                .Add(new Vector2(Camera.main.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y))
                .Add(PlayerMovement.Instance.IsCrouching())
                .Add(PlayerMovement.Instance.rb.velocity.sqrMagnitude > 1f)
                .Add(PlayerMovement.Instance.grounded);
            NetworkManager.client.Send(message);
        }

        public static void RequestScene()
        {
            NetworkManager.client.Send(Message.Create(MessageSendMode.Reliable, Packet_C2S.requestScene));
        }

        public static void Shoot(Vector3 from, Vector3 to)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_C2S.shoot);
            message
                .Add(from)
                .Add(to);
            NetworkManager.client.Send(message);
        }

        public static void Kill(ushort victim)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_C2S.kill);
            message.Add(victim);
            NetworkManager.client.Send(message);
        }
    }

    public class ClientHandle
    {
        private static bool PlayerList = false;

        [MessageHandler(Packet_S2C.initialPlayerList)]
        public static void InitialPlayerList(Message message)
        {
            int _length = message.GetInt();
            KillFeedGUI.AddText("There are " + _length + " other players online.");
            while (_length-- > 0)
            {
                ushort _id = message.GetUShort();
                string _username = message.GetString();
                if (PlaytimeLogic.players.Find(x => x.id == _id) != null) continue; // player already exists
                PlaytimeLogic.players.Add(new Player(_id, _username));
            }
            PlayerList = true;
        }

        [MessageHandler(Packet_S2C.addPlayer)]
        public static void AddPlayer(Message message)
        {
            bool join = message.GetBool();
            ushort id = message.GetUShort();
            if(!join)
            {
                Player _p = PlaytimeLogic.players.Find(p => p.id == id);
                if (_p == null) return; // player doesn't exist
                KillFeedGUI.AddText($"<color=red>({id}) {_p.username} disconnected</color>");
                _p.Destroy();
                PlaytimeLogic.players.Remove(_p);
                return;
            }
            string username = message.GetString();

            if (PlaytimeLogic.players.Find(x => x.id == id) != null) return; // player already exists
            PlaytimeLogic.players.Add(new Player(id, username));
            KillFeedGUI.AddText($"<color=green>({id}) {username} connected</color>");
        }

        [MessageHandler(Packet_S2C.playerData)]
        public static void PlayerData(Message message)
        {
            if (!PlayerList) return; // no player list yet
            ushort pid = message.GetUShort();
            Vector3 pos = message.GetVector3();
            Vector2 rot = message.GetVector2();
            bool crouching = message.GetBool();
            bool moving = message.GetBool();
            bool grounded = message.GetBool();
            Player p = (from x in PlaytimeLogic.players where x.id == pid select x).First();
            p.Move(pos, rot);
            p.UpdateAnimations(crouching, moving, grounded);
        }

        [MessageHandler(Packet_S2C.bullet)]
        public static void Bullet(Message message)
        {
            Vector3 from = message.GetVector3();
            Vector3 to = message.GetVector3();
            BulletRenderer.DrawBullet(from, to, Color.red);
        }

        [MessageHandler(Packet_S2C.killFeed)]
        public static void KillFeed(Message message)
        {
            string msg = message.GetString();
            KillFeedGUI.AddText(msg);
        }

        [MessageHandler(Packet_S2C.teleport)]
        public static void Teleport(Message message)
        {
            var pos = message.GetSerializable<MessageExtensions.oVector3>();
            var rot = message.GetSerializable<MessageExtensions.oVector2>();
            var vel = message.GetSerializable<MessageExtensions.oVector3>();
            if (pos.HasValue())
                PlayerMovement.Instance.transform.position = pos.GetValue();
            if(rot.HasValue())
            {
                Camera.main.transform.rotation = Quaternion.Euler(rot.GetValue().x, rot.GetValue().y, 0f);
                PlayerMovement.Instance.orientation.transform.rotation = Quaternion.Euler(0f, rot.GetValue().y, 0f);
            }
            if(vel.HasValue())
                PlayerMovement.Instance.rb.velocity = vel.GetValue();
        }

        [MessageHandler(Packet_S2C.map)]
        public static void MapChange(Message message)
        {
            bool isDefault = message.GetBool();
            string mapName = message.GetString();
            KillFeedGUI.AddText($"Loading map {mapName}");
            if (isDefault)
            {
                SceneManager.LoadScene(mapName);
                Game.Instance.StartGame();
                return;
            }
            int httpPort = message.GetInt();
            // download map from http server
            KME_LevelPlayer.LoadLevel(mapName, MapDownloader.DownloadMap(Loader.address, httpPort));
        }
    }
}
