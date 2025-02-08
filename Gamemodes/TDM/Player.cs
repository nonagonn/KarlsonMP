using ServerKMP;
using ServerKMP.GamemodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Default
{
    public class Player
    {
        public ushort id;
        public string username;
        public int hp;
        public int kills, deaths, score;
        public DateTime invicibleUntil;
        public int spectating = 0;

        public enum Team { Blue, Red, Warmup };
        public Team team;

        public Player(ushort _id, string _username)
        {
            id = _id;
            username = _username;
            hp = 100;
            kills = 0;
            deaths = 0;
            score = 0;
            invicibleUntil = DateTime.Now;
            spectating = 0;
            team = Team.Warmup;
        }

        public void SetUsername(string _username)
        {
            username = _username;
        }

        public void SetHP(int _hp)
        {
            hp = _hp;
            new MessageServerToClient.MessageSetHP(hp).Send(id);
        }

        public void RespawnPlayer()
        {
            // reset hp
            SetHP(100);
            if(team == Team.Warmup)
            {
                // pick random spawn location
                int count = MapManager.currentMap.map_data["spawn"].Count;
                var pos = MapManager.currentMap.map_data["spawn"][new Random().Next(count)];
                invicibleUntil = DateTime.Now.AddSeconds(3);
                new MessageServerToClient.MessageRespawn(pos.Item2).Send(id);
            }
            else
            {
                List<Vector3> spawnPos = new List<Vector3>();
                if(team == Team.Blue)
                {
                    foreach (var x in MapManager.currentMap.map_data["spawn"])
                        if (x.Item1.Contains("blue"))
                            spawnPos.Add(x.Item2);
                }
                else
                {
                    foreach (var x in MapManager.currentMap.map_data["spawn"])
                        if (x.Item1.Contains("red"))
                            spawnPos.Add(x.Item2);
                }
                int count = spawnPos.Count;
                var pos = spawnPos[new Random().Next(count)];
                new MessageServerToClient.MessageRespawn(pos).Send(id);
            }
        }

        public void TeleportToSpawn()
        {
            // pick random spawn location
            int count = MapManager.currentMap.map_data["spawn"].Count;
            var pos = MapManager.currentMap.map_data["spawn"][new Random().Next(count)];
            new MessageServerToClient.MessageTeleport(pos.Item2, Vector2.zero, Vector3.zero).Send(id);
        }

        public void EnterSpectate(ushort target)
        {
            new MessageServerToClient.MessageSpectate(target).Send(id);
            spectating = target;

            // send fake position
            new MessageServerToClient.MessagePositionData(id, new Vector3(30000f, 30000f, 30000f), Vector2.zero, false, false, false).SendToAll(id);
        }
        public void ExitSpectate()
        {
            new MessageServerToClient.MessageSpectate().Send(id);
            spectating = 0;
        }

        public void Destroy()
        {
            username = "";
        }

        public void ChangeTeam(Team newTeam)
        {
            team = newTeam;
            string modelColor;
            Vector3 bulletColor;
            switch(newTeam)
            {
                case Team.Blue:
                    modelColor = "blue";
                    bulletColor = new Vector3(0f, 0f, 1f);
                    break;
                case Team.Red:
                    modelColor = "red";
                    bulletColor = new Vector3(1f, 0f, 0f);
                    break;
                default:
                case Team.Warmup:
                    modelColor = "yellow";
                    bulletColor = new Vector3(0f, 0f, 1f);
                    break;
            }
            // update visuals
            new MessageServerToClient.MessageColorPlayer(id, modelColor).SendToAll(id);
            new MessageServerToClient.MessageSelfBulletColor(bulletColor).Send(id);
        }

        public void SendChatMessage(string message)
        {
            new MessageServerToClient.MessageChatMessage(message).Send(id);
        }
    }
}
