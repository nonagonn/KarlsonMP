using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerKMP.GamemodeApi;
using ServerKMP;
using System.IO.Ports;

namespace ServerKMP.Gamemodes.TDM
{
    public class GamemodeEntry : Gamemode
    {
        private static Dictionary<ushort, Action<MessageClientToServer.MessageBase_C2S>> messageHandlers = new Dictionary<ushort, Action<MessageClientToServer.MessageBase_C2S>>
            {
                { Packet_C2S.handshake, MessageHandlers.Handshake },
                { Packet_C2S.position, MessageHandlers.PositionData },
                { Packet_C2S.requestScene, MessageHandlers.RequestScene },
                { Packet_C2S.shoot, MessageHandlers.Shoot },
                { Packet_C2S.damage, MessageHandlers.Damage },
                { Packet_C2S.chat, MessageHandlers.Chat },
            };
        public static Dictionary<ushort, Player> players = new Dictionary<ushort, Player>();

        public static int MaxWarmupTime = 100; // in seconds
        public static DateTime WarmupEnd;
        public static bool TeamsAssigned = false;

        public override void OnStart()
        {
            WarmupEnd = DateTime.Now.AddSeconds(MaxWarmupTime);
            KMP_TaskScheduler.Schedule(() =>
            {
                // die everyone and specself, assign teams
                Player.Team nextTeam = Player.Team.Blue;
                foreach(var x in GamemodeEntry.players.Values)
                {
                    x.ChangeTeam(nextTeam);
                    new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.AboveCrosshair, "You are team " + (nextTeam == Player.Team.Blue ? "Blue" : "Red")).Send(x.id);
                    new MessageServerToClient.MessageDied(x.id).Send(x.id);
                    new MessageServerToClient.MessageSpectate(x.id).Send(x.id);

                    // reset warmup scores
                    x.kills = 0;
                    x.deaths = 0;
                    x.score = 0;

                    if (nextTeam == Player.Team.Blue)
                        nextTeam = Player.Team.Red;
                    else
                        nextTeam = Player.Team.Blue;
                }
                TeamsAssigned = true;
                new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.Subtitle, "Match will start in 5 seconds!").SendToAll();

                // show nametags of teammates

                var red_players = GamemodeEntry.players.Where(x => x.Value.team == Player.Team.Red).Select(x => x.Key).ToList();
                new MessageServerToClient.MessageShowNametags(true, red_players).SendToList(red_players);
                var blue_players = GamemodeEntry.players.Where(x => x.Value.team == Player.Team.Blue).Select(x => x.Key).ToList();
                new MessageServerToClient.MessageShowNametags(true, blue_players).SendToList(blue_players);

                KMP_TaskScheduler.Schedule(() =>
                {
                    new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.TopCenter, "<size=30><color=blue>0</color> <color=silver>-</color> <color=red>0</color></size>").SendToAll();
                    RoundManager.StartRound();
                }, DateTime.Now.AddSeconds(5));
            }, WarmupEnd);
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
            new MessageServerToClient.MessageKillFeed($"<color=red>({id}) {players[id].username} disconnected</color>").SendToAll();
            players[id].Destroy();
            players.Remove(id);
            // send player leave message
            new MessageServerToClient.MessagePlayerJoinLeave(id).SendToAll();

            // update scoreboard
            GamemodeEntry.UpdateScoreboard();
        }

        public override void OnMapChange()
        {
            if (MapManager.currentMap!.isDefault) // default map, just send scene name
                new MessageServerToClient.MessageMapChange(MapManager.currentMap.name).SendToAll();
            else // here we need to use the pre-implemented http server
                new MessageServerToClient.MessageMapChange(MapManager.currentMap.name, Config.HTTP_PORT).SendToAll();
        }

        public static void UpdateScoreboard()
        {
            new MessageServerToClient.MessageUpdateScoreboard(GamemodeEntry.players.Select(x => (x.Key, x.Value.username, x.Value.kills, x.Value.deaths, x.Value.score)).ToList()).SendToAll();
        }

        public override void ServerTick()
        {
            if(DateTime.Now.AddSeconds(1) < WarmupEnd)
            {
                new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.AboveCrosshair, "Warmup " + (WarmupEnd - DateTime.Now).ToString("mm\\:ss")).SendToAll();
            }
        }
    }
}
