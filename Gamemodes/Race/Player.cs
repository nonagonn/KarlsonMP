using ServerKMP;
using ServerKMP.GamemodeApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Race
{
    public class Player
    {
        public ushort id;
        public string username;
        public int spectating = 0;
        public bool admin;
        public Vector3 lastPos;
        public DateTime lastTimeInZone;
        public int score;
        public bool weapons;

        public Player(ushort _id, string _username)
        {
            id = _id;
            username = _username;
            spectating = 0;
            score = 0;
            admin = false;
            weapons = true;
        }

        public void SetUsername(string _username)
        {
            username = _username;
        }

        public void GiveWeapons()
        {
            new MessageServerToClient.MessageGiveTakeWeapon("ak47", new Vector3(50f, 50f, 2.5f), new Vector3(0f, 180f, 0f), new Vector3(-0.015f, -0f, 0f), Vector3.zero, "smg", 0.2f, 0.15f, 20, 1, 0.01f, 0.2f, 0, 40f, 0, 0.1f).Send(id);
            new MessageServerToClient.MessageGiveTakeWeapon("deagle", new Vector3(1.44f, 1.44f, 0.527f), new Vector3(-90f, 0f, 0f), new Vector3(-0.3f, -0.3f, 0f), new Vector3(0f, 0.2f, 0.2f), "pistol", 0.3f, 0.4f, 1, 1, 0, 0.7f, 0, 100f, 0, 0).Send(id);
        }

        public void TakeWeapons()
        {
            new MessageServerToClient.MessageGiveTakeWeapon(1).Send(id);
            new MessageServerToClient.MessageGiveTakeWeapon(0).Send(id);
        }

        public void RespawnPlayer()
        {
            var pos = MapManager.currentMap!.map_data["spawn"][0];
            new MessageServerToClient.MessageRespawn(pos.Item2).Send(id);
            if (weapons)
                GiveWeapons();
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

        public void EnableAdmin()
        {
            admin = true;
            username = "<color=#ee4444>(A)</color> <color=#ff9494>" + username + "</color>";
            new MessageServerToClient.MessageChatMessage($"<color=#c56cf5><b>»</b> {username} is now an admin</color>").SendToAll();
        }

        public void Destroy()
        {
            username = "";
        }
    }
}
