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
        public uint respawnTaskId;
        public bool respawnTaskActive;

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
            respawnTaskActive = false;
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
            // pick random spawn location
            int count = MapManager.currentMap.spawnPositions.Count;
            var pos = MapManager.currentMap.spawnPositions[new Random().Next(count)];
            invicibleUntil = DateTime.Now.AddSeconds(3);
            new MessageServerToClient.MessageRespawn(pos.Item2).Send(id);
        }

        public void TeleportToSpawn()
        {
            // pick random spawn location
            int count = MapManager.currentMap.spawnPositions.Count;
            var pos = MapManager.currentMap.spawnPositions[new Random().Next(count)];
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
            if (respawnTaskActive)
                KMP_TaskScheduler.CancelTask(respawnTaskId);
            username = "";
        }
    }
}
