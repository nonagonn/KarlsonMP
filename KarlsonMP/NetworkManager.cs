using Riptide;
using Riptide.Utils;
using System;
using System.Collections;
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
            client.ConnectionFailed += FailedToConnect;
            client.Disconnected += DidDisconnect;
        }

        public static void Quit()
        {
            if(!client.IsNotConnected)
                client.Disconnect();
        }

        private static void FailedToConnect(object sender, ConnectionFailedEventArgs e)
        {
            MonoHooks.ShowDialog("KarlsonMP reborn", "Failed to connect to the server", "Exit game", "", () => { Application.Quit(); });
        }

        private static void DidDisconnect(object sender, DisconnectedEventArgs e)
        {
            if(e.Message == null)
                MonoHooks.ShowDialog("KarlsonMP reborn", "Server closed the connection", "Exit game", "", () => { Application.Quit(); });
            else
                MonoHooks.ShowDialog("KarlsonMP reborn", "Server closed the connection:\n\n" + e.Message.GetString(), "Exit game", "", () => { Application.Quit(); });
        }
    }

    public static class Packet_S2C
    {
        public const ushort encryptionKey = 1; // used for sending discord bearer token
        public const ushort initialPlayerList = 2; // initial player list
        public const ushort addPlayer = 3; // player join/left
        public const ushort playerData = 4; // server snapshot of the world
        public const ushort bullet = 5; // bullet ray
        public const ushort killFeed = 6;
        public const ushort teleport = 7;
        public const ushort map = 8;
        public const ushort hp = 9;
        public const ushort kill = 10; // we killed someone
        public const ushort death = 11; // we died
        public const ushort respawn = 12; // server respawned us (teleport should come before/after)
        public const ushort chat = 13;
        public const ushort scoreboard = 14;
        public const ushort colorPlayer = 15; // color: 'yellow', 'red', 'blue'
        public const ushort spectate = 16; // spectate player / exit spectate
        public const ushort hudMessage = 17;
        public const ushort selfBulletColor = 18; // change own bullet color
        public const ushort showNametags = 19;
    }
    public static class Packet_C2S
    {
        public const ushort handshake = 1; // handshake, send discord bearer
        public const ushort position = 2; // position
        public const ushort requestScene = 3; // request sync and initialPlayerList
        public const ushort shoot = 4; // client shoot
        public const ushort damage = 5; // damage report after shoot
        public const ushort chat = 6;
    }

    public class ClientSend
    {
        public static void Handshake(byte[] encryptedDiscordToken)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_C2S.handshake);
            message.Add(encryptedDiscordToken);
            message.Add(Loader.discord_user.Id);
            NetworkManager.client.Send(message);
        }

        public static void PositionData()
        {
            if (!ClientHandle.PlayerList) return;
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

        public static void Damage(ushort victim, int damage)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_C2S.damage);
            message
                .Add(victim)
                .Add(damage);
            NetworkManager.client.Send(message);
        }

        public static void ChatMessage(string msg)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_C2S.chat);
            message.Add(msg);
            NetworkManager.client.Send(message);
        }
    }

    public class ClientHandle
    {
        public static bool PlayerList { get; private set; } = false;

        [MessageHandler(Packet_S2C.encryptionKey)]
        public static void EncryptionKey(Message message)
        {
            byte[] blob = message.GetBytes();

            void EncryptHandshake()
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportCspBlob(blob);
                    rsa.PersistKeyInCsp = false;
                    byte[] discordTokenEcrypted = rsa.Encrypt(Encoding.ASCII.GetBytes(Loader.discord_token), false);
                    ClientSend.Handshake(discordTokenEcrypted);
                }
            }

            if(!Loader.OnLinux)
            {
                IEnumerator waitForDiscord()
                {
                    KillFeedGUI.AddText("Waiting for discord");
                    while (Loader.discord_token == null || Loader.discord_user.Id == 0)
                        yield return new WaitForSecondsRealtime(0.1f);
                    KillFeedGUI.AddText("Sending discord account");
                    EncryptHandshake();
                }
                Loader.monoHooks.StartCoroutine(waitForDiscord());
            }
            else
            {
                KillFeedGUI.AddText("Sending discord account");
                EncryptHandshake();
            }
        }

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
                _p.Destroy();
                PlaytimeLogic.players.Remove(_p);
                return;
            }
            string username = message.GetString();

            if (PlaytimeLogic.players.Find(x => x.id == id) != null) return; // player already exists
            PlaytimeLogic.players.Add(new Player(id, username));
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
            var c = message.GetVector3();
            Color color = new Color(c.x, c.y, c.z);
            BulletRenderer.DrawBullet(from, to, color);
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
            // clear old player list
            PlaytimeLogic.players.Clear();
            PlayerList = false;
            bool isDefault = message.GetBool();
            string mapName = message.GetString();
            KillFeedGUI.AddText($"Loading map {mapName}");
            if (isDefault)
            {
                SceneManager.LoadScene(mapName);
                Game.Instance.StartGame();
                return;
            }
            ushort httpPort = message.GetUShort();
            // download map from http server
            KME_LevelPlayer.LoadLevel(mapName, MapDownloader.DownloadMap(Loader.address, httpPort));
        }

        [MessageHandler(Packet_S2C.hp)]
        public static void SetHP(Message message)
        {
            int hp = message.GetInt();
            if (hp < PlaytimeLogic.hp) KMP_AudioManager.PlaySound("hitself", 0.15f);
            PlaytimeLogic.hp = hp;
        }

        [MessageHandler(Packet_S2C.kill)]
        public static void ConfirmKill(Message message)
        {
            ushort victim = message.GetUShort();
            KMP_AudioManager.PlaySound("kill", 0.05f);
        }

        [MessageHandler(Packet_S2C.death)]
        public static void WeDied(Message message)
        {
            ushort killer = message.GetUShort();
            // if killer = 0, natural cause
            KMP_AudioManager.PlaySound("death", 0.05f);
            PlayerMovement.Instance.ReflectionSet("dead", true);
        }

        [MessageHandler(Packet_S2C.respawn)]
        public static void Respawn(Message message)
        {
            if (PlaytimeLogic.spectatingId != 0)
                PlaytimeLogic.ExitSpectate();
            PlayerMovement.Instance.ReflectionSet("dead", false);
            PlaytimeLogic.suicided = false;
            // reset guns ammo, because we got respawned
            Inventory.ReloadAll();
            Vector3 position = message.GetVector3();
            PlayerMovement.Instance.transform.position = position;
            PlayerMovement.Instance.rb.velocity = Vector3.zero;
            PlayerMovement.Instance.ReflectionInvoke("StopCrouch");
        }

        [MessageHandler(Packet_S2C.chat)]
        public static void Chat(Message message)
        {
            string msg = message.GetString();
            PlaytimeLogic.AddChat(msg);
        }

        [MessageHandler(Packet_S2C.scoreboard)]
        public static void ScoreboardUpdate(Message message)
        {
            Scoreboard.UpdateScoreboard(message);
        }

        [MessageHandler(Packet_S2C.colorPlayer)]
        public static void ColorPlayer(Message message)
        {
            ushort who = message.GetUShort();
            if (who == NetworkManager.client.Id) return;
            string color = message.GetString();
            Texture2D tex;
            switch(color)
            {
                case "yellow":
                    tex = KME_LevelPlayer.gameTex[5];
                    break;
                case "blue":
                    tex = KME_LevelPlayer.gameTex[7];
                    break;
                case "red":
                    tex = KME_LevelPlayer.gameTex[9];
                    break;
                default:
                    return;
            }
            Player p = (from x in PlaytimeLogic.players where x.id == who select x).First();
            p.player.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material.mainTexture = tex;
        }

        [MessageHandler(Packet_S2C.spectate)]
        public static void Spectate(Message message)
        {
            bool enterSpec = message.GetBool();
            if (!enterSpec)
                PlaytimeLogic.ExitSpectate();
            else
                PlaytimeLogic.StartSpectate(message.GetUShort());
        }

        [MessageHandler(Packet_S2C.hudMessage)]
        public static void HudMessage(Message message)
        {
            int position = message.GetInt();
            string text = message.GetString();
            if (position < 0 || position > 3) return;
            switch (position)
            {
                case 0:
                    HUDMessages.topCenter = text;
                    break;
                case 1:
                    HUDMessages.aboveCrosshair = text;
                    break;
                case 2:
                    HUDMessages.subtitle = text;
                    break;
                case 3:
                    HUDMessages.bottomLeft = text;
                    break;
            }
        }

        [MessageHandler(Packet_S2C.selfBulletColor)]
        public static void SelfBulletColor(Message message)
        {
            Vector3 c = message.GetVector3();
            Inventory.selfBulletColor = new Color(c.x, c.y, c.z);
        }

        [MessageHandler(Packet_S2C.showNametags)]
        public static void ShowNametags(Message message)
        {
            bool toggle = message.GetBool();
            if(message.UnreadLength > 0)
            {
                // player list toggle
                while(message.UnreadLength > 0)
                {
                    ushort pid = message.GetUShort();
                    var p = (from x in PlaytimeLogic.players where x.id == pid select x).First();
                    if (p == null) continue;
                    p.nametagShown = toggle;
                }
            }
            else
            {
                PlaytimeLogic.showNametags = toggle;
                // global toggle
                foreach (var p in PlaytimeLogic.players)
                    p.nametagShown = PlaytimeLogic.showNametags;
            }
        }
    }
}
