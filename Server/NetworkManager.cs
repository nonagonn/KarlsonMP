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

namespace ServerKMP
{
    public static class NetworkManager
    {
        public static Server server;
        public static int CurrentTick { get; private set; } = 0;

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
            GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.OnPlayerDisconnect(e.Client.Id));
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
        public const ushort scoreboard = 13;
        public const ushort colorPlayer = 14; // color: 'yellow', 'red', 'blue'
        public const ushort spectate = 15; // spectate player / exit spectate
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
        [MessageHandler(Packet_C2S.handshake)]
        public static void Handshake(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.ProcessMessage(new MessageClientToServer.MessageHandshake(from, message)));
        [MessageHandler(Packet_C2S.position)]
        public static void PositionData(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.ProcessMessage(new MessageClientToServer.MessagePositionData(from, message)));
        [MessageHandler(Packet_C2S.requestScene)]
        public static void RequestScene(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.ProcessMessage(new MessageClientToServer.MessageRequestScene(from, message)));
        [MessageHandler(Packet_C2S.shoot)]
        public static void Shoot(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.ProcessMessage(new MessageClientToServer.MessageShoot(from, message)));
        [MessageHandler(Packet_C2S.damage)]
        public static void Damage(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.ProcessMessage(new MessageClientToServer.MessageDamage(from, message)));
        [MessageHandler(Packet_C2S.chat)]
        public static void Chat(ushort from, Message message)
            => GamemodeManager.SafeCall(() => GamemodeManager.currentGamemode.ProcessMessage(new MessageClientToServer.MessageChat(from, message)));
    }
}
