using ServerKMP;
using ServerKMP.GamemodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Default
{
    public static class MessageHandlers
    {
        public static void Handshake(MessageClientToServer.MessageBase_C2S _base)
            => Handshake((MessageClientToServer.MessageHandshake)_base);
        public static void Handshake(MessageClientToServer.MessageHandshake handshake)
        {
            GamemodeEntry.players.Add(handshake.fromId, new Player(handshake.fromId, handshake.username));

            // send playerjoin to all except client
            new MessageServerToClient.MessagePlayerJoinLeave(handshake.fromId, handshake.username).SendToAll(handshake.fromId);
            // send player current map
            if (MapManager.currentMap.isDefault) // default map, just send scene name
                new MessageServerToClient.MessageMapChange(MapManager.currentMap.name).Send(handshake.fromId);
            else // here we need to use the pre-implemented http server
                new MessageServerToClient.MessageMapChange(MapManager.currentMap.name, Config.HTTP_PORT).Send(handshake.fromId);

            // update scoreboard
            GamemodeEntry.UpdateScoreboard();

            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.BottomLeft, "KarlsonMP / Team DeathMatch").Send(handshake.fromId);
        }

        public static void PositionData(MessageClientToServer.MessageBase_C2S _base)
            => PositionData((MessageClientToServer.MessagePositionData)_base);
        public static void PositionData(MessageClientToServer.MessagePositionData positionData)
        {
            if (GamemodeEntry.players[positionData.fromId].spectating != 0) return; // player is spectating, ignore their position
            // broadcast position to all except client
            new MessageServerToClient.MessagePositionData(positionData).SendToAll(positionData.fromId);
            // here we use the constructor i conviniently created to transfer all fields from client message to server message
        }

        public static void RequestScene(MessageClientToServer.MessageBase_C2S _base)
        {
            // send initial player list (also filter by id, so client is not included in list)
            new MessageServerToClient.MessageInitialPlayerList(GamemodeEntry.players.Where(x => x.Key != _base.fromId).Select(x => (x.Key, x.Value.username)).ToList()).Send(_base.fromId);
            // teleport to spawn
            GamemodeEntry.players[_base.fromId].TeleportToSpawn();
        }

        public static void Shoot(MessageClientToServer.MessageBase_C2S _base)
            => Shoot((MessageClientToServer.MessageShoot)_base);
        public static void Shoot(MessageClientToServer.MessageShoot shoot)
        {
            Vector3 color = new Vector3(1f, 0f, 0f);
            if(DateTime.Now > GamemodeEntry.WarmupEnd)
            {
                // warmup finished, switch bullet color to team color
                if (GamemodeEntry.players[shoot.fromId].team == Player.Team.Blue)
                    color = new Vector3(0f, 0f, 1f);
                // else color is already red
            }
            // broadcast bullet to all players.
            // 1f, 0f, 0f is red color
            new MessageServerToClient.MessageSendBullet(shoot.origin, shoot.hitPoint, color).SendToAll(shoot.fromId);
        }

        public static void Damage(MessageClientToServer.MessageBase_C2S _base)
            => Damage((MessageClientToServer.MessageDamage)_base);
        public static void Damage(MessageClientToServer.MessageDamage damage)
        {
            if (GamemodeEntry.players[damage.victim].invicibleUntil > DateTime.Now) return; // victim is invincible
            if (damage.damage < 0) return; // damage is negative. they are cheating
            if (GamemodeEntry.players[damage.fromId].team != Player.Team.Warmup && GamemodeEntry.players[damage.fromId].team == GamemodeEntry.players[damage.victim].team) return; // friendly fire
            // clamp hp to 0 (if damage is greater than health)
            GamemodeEntry.players[damage.victim].SetHP(Math.Max(0, GamemodeEntry.players[damage.victim].hp - damage.damage));
            if(GamemodeEntry.players[damage.victim].hp == 0)
            { // victim died
                if(damage.fromId != damage.victim)
                { // if not suicide
                    GamemodeEntry.players[damage.fromId].kills++;
                    GamemodeEntry.players[damage.fromId].score += 100;
                }
                GamemodeEntry.players[damage.victim].deaths++;
                GamemodeEntry.players[damage.victim].score -= 50;
                GamemodeEntry.UpdateScoreboard();
                string kfMessage = $"{GamemodeEntry.players[damage.fromId].username} killed {GamemodeEntry.players[damage.victim].username}";
                if (damage.fromId == damage.victim)
                    kfMessage = $"{GamemodeEntry.players[damage.fromId].username} commited suicide";
                new MessageServerToClient.MessageKillFeed(kfMessage).SendToAll();
                new MessageServerToClient.MessageConfirmKill(damage.victim).Send(damage.fromId);

                if (DateTime.Now < GamemodeEntry.WarmupEnd)
                {
                    // we are in warmup, respawn player instantly
                    GamemodeEntry.players[damage.victim].RespawnPlayer();
                    return;
                }

                new MessageServerToClient.MessageDied(damage.fromId).Send(damage.victim);

                bool playersAliveOnMyTeam = false;
                ushort specid = 0;
                foreach(var x in GamemodeEntry.players.Values)
                {
                    if (x.id == damage.victim) continue; // duh
                    if (x.team == GamemodeEntry.players[damage.victim].team && x.spectating == 0)
                    {
                        playersAliveOnMyTeam = true;
                        specid = x.id;
                        GamemodeEntry.players[damage.victim].EnterSpectate(x.id);
                        break;
                    }
                }
                if(!playersAliveOnMyTeam)
                {
                    RoundManager.EndRound(GamemodeEntry.players[damage.fromId].team);
                }
                else
                {
                    // check if other team-mates were spectating me, and switch to other alive
                    foreach (var x in GamemodeEntry.players.Values)
                    {
                        if (x.spectating == damage.victim)
                        {
                            x.EnterSpectate(specid);
                        }
                    }
                }
            }
        }

        public static void Chat(MessageClientToServer.MessageBase_C2S _base)
            => Chat((MessageClientToServer.MessageChat)_base);
        public static void Chat(MessageClientToServer.MessageChat chat)
        {
            string msg = chat.message.Replace("<", "<<i></i>"); // sanitize against unwanted richtext
            Console.WriteLine($"[CHAT] {GamemodeEntry.players[chat.fromId].username} : {msg}");
            new MessageServerToClient.MessageChatMessage($"{GamemodeEntry.players[chat.fromId].username} : {msg}").SendToAll();
        }
    }
}
