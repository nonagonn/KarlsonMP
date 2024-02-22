using Riptide.Utils;
using Riptide;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ServerKMP
{
    public static class NetworkManager
    {
        public static Server server;
        public static int CurrentTick { get; private set; } = 0;
        public static Dictionary<int, Player> clients = new Dictionary<int, Player>();

        public static void Start()
        {
            Console.WriteLine($"Starting server (port {Config.PORT}, 16 max clients)");
            RiptideLogger.Initialize(Console.WriteLine, false);
            server = new Server("Riptide");
            server.Start(Config.PORT, 16);
            server.ClientDisconnected += Server_ClientDisconnected;
        }

        private static void Server_ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            clients[e.Client.Id].Destroy();
            clients.Remove(e.Client.Id);
            ServerSend.PlayerLeave(e.Client.Id);
        }

        public static void Update()
        {
            server.Update();
        }

        public static void Exit()
        {
            server.Stop();
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
        public const ushort hp = 8;
        public const ushort kill = 9; // we killed someone
        public const ushort death = 10; // we died
        public const ushort respawn = 11; // server respawned us (teleport should come next)
        public const ushort chat = 12;
    }
    public static class Packet_C2S
    {
        public const ushort handshake = 1; // handshake, send username
        public const ushort position = 2; // position
        public const ushort requestScene = 3; // request sync and initialPlayerList
        public const ushort shoot = 4; // client shoot
        public const ushort damage = 5; // damage report after shoot
        public const ushort chat = 6;
    }

    public class ServerSend
    {
        public static void InitialPlayerList(ushort _id)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.initialPlayerList);
            message.Add(NetworkManager.clients.Count - 1);
            foreach (var c in NetworkManager.clients.Values)
            {
                if (c.id == _id) continue; // skip self
                message.Add(c.id);
                message.Add(c.username);
            }
            NetworkManager.server.Send(message, _id);
        }

        public static void PlayerJoin(ushort _id)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.addPlayer);
            message
                .Add(true)
                .Add(_id)
                .Add(NetworkManager.clients[_id].username);
            NetworkManager.server.SendToAll(message, _id);
        }
        public static void PlayerLeave(ushort _id)
        {

            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.addPlayer);
            message
                .Add(false)
                .Add(_id);
            NetworkManager.server.SendToAll(message);
        }

        public static void SendBullet(Vector3 from, Vector3 to, ushort original)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.bullet);
            message
                .Add(from)
                .Add(to);
            NetworkManager.server.SendToAll(message, original);
        }

        public static void KillFeed(int killer, int victim)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.killFeed);
            message.Add($"{NetworkManager.clients[killer].username} killed {NetworkManager.clients[victim].username}");
            NetworkManager.server.SendToAll(message);
        }

        public static void Teleport(ushort id, MessageExtensions.oVector3 pos, MessageExtensions.oVector2 rot, MessageExtensions.oVector3 vel)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.teleport);
            message.Add(pos);
            message.Add(rot);
            message.Add(vel);
            NetworkManager.server.Send(message, id);
        }

        private static Message MakeMapChangeMessage()
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.map);
            message.AddBool(MapManager.currentMap.isDefault);
            message.AddString(MapManager.currentMap.name);
            if (!MapManager.currentMap.isDefault)
                message.AddInt(Config.HTTP_PORT); // map downloader port
            return message;
        }
        public static void MapChange() => NetworkManager.server.SendToAll(MakeMapChangeMessage());
        public static void MapChange(ushort id) => NetworkManager.server.Send(MakeMapChangeMessage(), id);

        public static void SetHP(ushort id, int hp)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.hp);
            message.AddInt(hp);
            NetworkManager.server.Send(message, id);
        }

        public static void BroadcastKill(ushort killer, ushort victim)
        {
            ConfirmKill(killer, victim);
            Died(victim, killer);
        }

        public static void ConfirmKill(ushort killer, ushort victim)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.kill);
            message.Add(victim);
            NetworkManager.server.Send(message, killer);
        }

        public static void Died(ushort victim, ushort killer)
        {
            // if natural, killer = 0
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.death);
            message.Add(killer);
            NetworkManager.server.Send(message, victim);
        }

        public static void Respawn(ushort id)
        {
            // tell player we are going to respawn them
            NetworkManager.server.Send(Message.Create(MessageSendMode.Reliable, Packet_S2C.respawn), id);
        }

        public static void ChatMessage(string msg)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.chat);
            message.Add(msg);
            NetworkManager.server.SendToAll(message);
        }
        public static void ChatMessage(string msg, ushort target)
        {
            Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.chat);
            message.Add(msg);
            NetworkManager.server.Send(message, target);
        }
    }

    public class ServerHandle
    {
        [MessageHandler(Packet_C2S.handshake)]
        public static void Handshake(ushort from, Message message)
        {
            string _username = message.GetString();
            NetworkManager.clients.Add(from, new Player(from));
            NetworkManager.clients[from].SetUsername(_username);
            ServerSend.PlayerJoin(from);
            ServerSend.MapChange(from);
        }

        [MessageHandler(Packet_C2S.position)]
        public static void KeyboardState(ushort from, Message message)
        {
            Message broadcast = Message.Create(MessageSendMode.Unreliable, Packet_S2C.playerData);
            broadcast.Add(from);
            broadcast.Add(message.GetVector3());
            broadcast.Add(message.GetVector2());
            broadcast.Add(message.GetBool());
            broadcast.Add(message.GetBool());
            broadcast.Add(message.GetBool());
            NetworkManager.server.SendToAll(broadcast, from);
        }

        [MessageHandler(Packet_C2S.requestScene)]
        public static void RequestScene(ushort from, Message _)
        {
            ServerSend.InitialPlayerList(from);
            NetworkManager.clients[from].TeleportToSpawn();
        }

        [MessageHandler(Packet_C2S.shoot)]
        public static void Shoot(ushort fromId, Message message)
        {
            Vector3 from = message.GetVector3();
            Vector3 to = message.GetVector3();
            Message broadcast = Message.Create(MessageSendMode.Reliable, Packet_S2C.bullet);
            broadcast.Add(from);
            broadcast.Add(to);
            NetworkManager.server.SendToAll(broadcast, fromId);
        }

        [MessageHandler(Packet_C2S.damage)]
        public static void Damage(ushort from, Message message)
        {
            ushort who = message.GetUShort();
            if (NetworkManager.clients[who].invicibleUntil > DateTime.Now) return; // player is invincible
            int damage = message.GetInt();
            NetworkManager.clients[who].hp -= damage;
            if(NetworkManager.clients[who].hp <= 0)
            {
                ServerSend.KillFeed(from, who);
                ServerSend.BroadcastKill(from, who);
                NetworkManager.clients[who].MarkRespawn();
                NetworkManager.clients[who].TeleportToSpawn();
                NetworkManager.clients[who].hp = 100;
                ServerSend.SetHP(who, 100);
            }
            else
            {
                ServerSend.SetHP(who, NetworkManager.clients[who].hp);
            }
        }

        [MessageHandler(Packet_C2S.chat)]
        public static void Chat(ushort from, Message message)
        {
            string msg = message.GetString();
            msg = msg.Replace("<", "<<i></i>"); // sanitize against unwanted richtext
            ServerSend.ChatMessage($"{NetworkManager.clients[from].username} : {msg}");
            Console.WriteLine($"[CHAT] {NetworkManager.clients[from].username} : {msg}");
        }
    }
}
