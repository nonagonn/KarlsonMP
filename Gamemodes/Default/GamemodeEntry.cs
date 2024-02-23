using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerKMP.GamemodeApi;
using ServerKMP;

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
                { Packet_C2S.handshake, MessageHandlers.Handshake }
            };
            players = new Dictionary<ushort, Player>();
        }
    }
}
