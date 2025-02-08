using Riptide.Utils;
using Riptide;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ServerKMP.GamemodeApi;
using System.Net;
using ServerNET_CORE;

namespace ServerKMP
{
    public static class NetworkManager
    {
        public static Server? server;
        public static int CurrentTick { get; private set; } = 0;
        public static HashSet<ushort> clientsAwaitingHandshake = new HashSet<ushort>(); // players that need to send handshake
        public static HashSet<ushort> registeredOnGamemode = new HashSet<ushort>(); // players that were sent to gamemode to be processed via C2S handshake

        public static void Start()
        {
            Console.WriteLine($"Starting server (port {Config.PORT}, {Config.MAX_PLAYERS} max clients)");
            RiptideLogger.Initialize(Console.WriteLine, false);
            server = new Server(new Riptide.Transports.Udp.UdpServer(Riptide.Transports.Udp.SocketMode.IPv4Only), "Riptide");
            //server = new Server("Riptide");
            server.Start(Config.PORT, Config.MAX_PLAYERS);
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
        }

        private static void Server_ClientConnected(object? sender, ServerConnectedEventArgs e)
        {
            Message encryptionKey = Message.Create(MessageSendMode.Reliable, Packet_S2C.encryptionKey);
            encryptionKey.Add(Program.RSA_blob);
            server!.Send(encryptionKey, e.Client);
            clientsAwaitingHandshake.Add(e.Client.Id);
            KMP_TaskScheduler.Schedule(() =>
            {
                if (!e.Client.IsConnected) return; // client alr disconnected
                if (clientsAwaitingHandshake.Contains(e.Client.Id))
                    server.DisconnectClient(e.Client, Message.Create().Add("Didn't respond to handshake."));
            }, DateTime.Now.AddSeconds(30));
            Console.WriteLine("[DISCORD_LOGIN] Sent RSA key to " + e.Client.Id);
            ServerStatus.SetServerStatus(1, "**Players**          " + server.ClientCount + "/" + Config.MAX_PLAYERS);
        }

        private static void Server_ClientDisconnected(object? sender, ServerDisconnectedEventArgs e)
        {
            if(clientsAwaitingHandshake.Contains(e.Client.Id))
                clientsAwaitingHandshake.Remove(e.Client.Id);
            if(registeredOnGamemode.Contains(e.Client.Id))
            {
                registeredOnGamemode.Remove(e.Client.Id);
                GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.OnPlayerDisconnect(e.Client.Id));
            }
            ServerStatus.SetServerStatus(1, "**Players**          " + (server!.ClientCount) + "/" + Config.MAX_PLAYERS);
        }

        public static void Update()
        {
            server!.Update();
        }

        public static void Exit()
        {
            server!.Stop();
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
        public const ushort handshake = 1; // handshake, send username
        public const ushort position = 2; // position
        public const ushort requestScene = 3; // request sync and initialPlayerList
        public const ushort shoot = 4; // client shoot
        public const ushort damage = 5; // damage report after shoot
        public const ushort chat = 6;
    }

    public class ServerHandle
    {
        // TODO: authenticate user in handshake, and pass to legacy
        [MessageHandler(Packet_C2S.handshake)]
        public static void Handshake(ushort from, Message message)
        {
            if(Config.IGNORE_DISCORD)
            {
                byte[] usernameEncrypted = message.GetBytes();
                // decrypt token
                string username = Encoding.ASCII.GetString(Program.RSA.Decrypt(usernameEncrypted, false));
                Console.WriteLine("[DISCORD_LOGIN] Setting username " + username);
                NetworkManager.clientsAwaitingHandshake.Remove(from);
                NetworkManager.registeredOnGamemode.Add(from);
                GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessageHandshake(from, username)));
                return;
            }
            Console.WriteLine("[DISCORD_LOGIN] Attempting to log in " + from);
            byte[] bearerTokenEncrypted = message.GetBytes();
            ulong discordId = message.GetULong();
            // decrypt token
            string bearerToken = Encoding.ASCII.GetString(Program.RSA.Decrypt(bearerTokenEncrypted, false));
            // check agains api
            using(HttpClient hc = new HttpClient())
            using(HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, Config.DISCORD_API + "/login.php"))
            {
                //req.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                req.Content = new StringContent("bearer=" + bearerToken + "&userid=" + discordId, Encoding.ASCII, "application/x-www-form-urlencoded");
                var res = hc.Send(req);
                var response = res.Content.ReadAsStringAsync().GetAwaiter().GetResult().Split(','); Console.WriteLine(string.Join(",", response));
                string result = response[0], username = response[1];
                if (result != "OK")
                {
                    Console.WriteLine("[DISCORD_LOGIN] Login ERROR for " + from + " (" + username + ")");
                    NetworkManager.server!.DisconnectClient(from, Message.Create().Add("Discord authentication error.\nPlease retry"));
                    return;
                }
                Console.WriteLine("[DISCORD_LOGIN] Login OK for " + from + " -> " + username);
                NetworkManager.clientsAwaitingHandshake.Remove(from);
                NetworkManager.registeredOnGamemode.Add(from);
                GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessageHandshake(from, username)));
            }
        }
        [MessageHandler(Packet_C2S.position)]
        public static void PositionData(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessagePositionData(from, message)));
        [MessageHandler(Packet_C2S.requestScene)]
        public static void RequestScene(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessageRequestScene(from, message)));
        [MessageHandler(Packet_C2S.shoot)]
        public static void Shoot(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessageShoot(from, message)));
        [MessageHandler(Packet_C2S.damage)]
        public static void Damage(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessageDamage(from, message)));
        [MessageHandler(Packet_C2S.chat)]
        public static void Chat(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode!.ProcessMessage(new MessageClientToServer.MessageChat(from, message)));
    }
}
