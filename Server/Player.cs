using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public class Player
    {
        public ushort id;
        public string username;
        public int hp;
        public DateTime invicibleUntil;

        public Player(ushort _id)
        {
            id = _id;
            username = "<nil>";
            hp = 100;
            invicibleUntil = DateTime.Now;
        }

        public void SetUsername(string _username)
        {
            username = _username;
        }

        public void MarkRespawn()
        {
            invicibleUntil = DateTime.Now.AddSeconds(3);
            ServerSend.Respawn(id);
        }

        public void TeleportToSpawn()
        {
            // pick random spawn location
            int count = MapManager.currentMap.spawnPositions.Count;
            var pos = MapManager.currentMap.spawnPositions[new Random().Next(count)];
            ServerSend.Teleport(id, pos.Item2, Vector2.zero, Vector3.zero);
        }

        public void Destroy()
        {
            username = "";
        }
    }
}
