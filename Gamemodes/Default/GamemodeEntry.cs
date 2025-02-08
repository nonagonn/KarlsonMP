using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerKMP.GamemodeApi;
using ServerKMP;
using System.IO.Ports;

namespace Default
{
    public class GamemodeEntry : Gamemode
    {
        private static Dictionary<ushort, Action<MessageClientToServer.MessageBase_C2S>> messageHandlers;
        public static Dictionary<ushort, Player> players;

        public override void OnStart()
        {
            messageHandlers = new Dictionary<ushort, Action<MessageClientToServer.MessageBase_C2S>>
            {
                { Packet_C2S.handshake, MessageHandlers.Handshake },
                { Packet_C2S.position, MessageHandlers.PositionData },
                { Packet_C2S.requestScene, MessageHandlers.RequestScene },
                { Packet_C2S.shoot, MessageHandlers.Shoot },
                { Packet_C2S.damage, MessageHandlers.Damage },
                { Packet_C2S.chat, MessageHandlers.Chat },
            };
            players = new Dictionary<ushort, Player>();
        }

        public override void ProcessMessage(MessageClientToServer.MessageBase_C2S message)
        {
            if (!messageHandlers.ContainsKey(message.RiptideId))
            {
                Console.WriteLine("[WARNING] Received known packet, but not registered in messageHandlers dictionary.");
                Console.WriteLine("[WARNING] Packet ID: " + message.RiptideId + " . Sent by client: " + message.fromId);
            }
            else
            {
                messageHandlers[message.RiptideId](message);
            }
        }

        public override void OnPlayerDisconnect(ushort id)
        {
            players[id].Destroy();
            players.Remove(id);
            // send player leave message
            new MessageServerToClient.MessagePlayerJoinLeave(id).SendToAll();

            // update scoreboard
            GamemodeEntry.UpdateScoreboard();
        }

        public override void OnMapChange()
        {
            if (MapManager.currentMap.isDefault) // default map, just send scene name
                new MessageServerToClient.MessageMapChange(MapManager.currentMap.name).SendToAll();
            else // here we need to use the pre-implemented http server
                new MessageServerToClient.MessageMapChange(MapManager.currentMap.name, Config.HTTP_PORT).SendToAll();
        }

        public static void UpdateScoreboard()
        {
            new MessageServerToClient.MessageUpdateScoreboard(GamemodeEntry.players.Select(x => (x.Key, x.Value.username, x.Value.kills, x.Value.deaths, x.Value.score)).ToList()).SendToAll();
        }
    }
}
