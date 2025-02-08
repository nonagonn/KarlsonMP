using ServerKMP.GamemodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Default
{
    public class RoundManager
    {
        public static void StartRound()
        {
            foreach(var x in GamemodeEntry.players.Values)
            {
                x.ExitSpectate();
                x.RespawnPlayer();
            }
            new MessageServerToClient.MessageKillFeed("Good luck!").SendToAll();
            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.AboveCrosshair, "").SendToAll();
            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.Subtitle, "").SendToAll();
        }

        private static int pointsBlue = 0, pointsRed = 0;

        public static void EndRound(Player.Team winningTeam)
        {
            // die and spec self
            foreach(var x in GamemodeEntry.players.Values)
            {
                if(x.spectating == 0)
                { // if player is still alive
                    new MessageServerToClient.MessageDied(x.id).Send(x.id);
                    new MessageServerToClient.MessageSpectate(x.id).Send(x.id);
                }
            }
            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.AboveCrosshair, "Team " + (winningTeam == Player.Team.Blue ? "<color=blue>Blue</color>" : "<color=red>Red</color>") + " won this round!").SendToAll();
            if(winningTeam == Player.Team.Blue)
                pointsBlue++;
            else
                pointsRed++;
            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.TopCenter, "<size=30><color=blue>" + pointsBlue + "</color> <color=silver>-</color> <color=red>" + pointsRed + "</color></size>").SendToAll();
            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.Subtitle, "Next round will start in 5 seconds!").SendToAll();

            KMP_TaskScheduler.Schedule(() => StartRound(), DateTime.Now.AddSeconds(5));
        }
    }
}
