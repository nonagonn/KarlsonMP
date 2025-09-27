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
        public ushort spectating = 0;
        public bool admin;
        public DateTime lastTimeInZone;
        public int score;
        public bool weapons;
        public bool show_hp;
        public bool in_zone;
        public bool sounds;
        public int pb;

        public Player(ushort _id, string _username)
        {
            id = _id;
            username = _username;
            spectating = 0;
            score = 0;
            admin = false;
            weapons = true;
            show_hp = true;
            in_zone = false;
            sounds = true;
            pb = 0;
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
            new MessageServerToClient.MessageGiveTakeWeapon(0).Send(id);
            new MessageServerToClient.MessageGiveTakeWeapon(0).Send(id);
        }

        public void RespawnPlayer()
        {
            if (show_hp)
                new MessageServerToClient.MessageSetHP(100).Send(id);
            else
                new MessageServerToClient.MessageSetHP(0).Send(id);
            var pos = MapManager.currentMap!.map_data["spawn"][0];
            new MessageServerToClient.MessageRespawn(pos.Item2).Send(id);
            if (weapons)
                GiveWeapons();
            NetworkManager.broadcastPosition[id] = true;
        }

        public void EnterSpectate(ushort target)
        {
            new MessageServerToClient.MessageSpectate(target).Send(id);
            spectating = target;

            new MessageServerToClient.MessageHUDMessage(MessageServerToClient.MessageHUDMessage.ScreenPos.TopCenter, $"<size=25>Spectating {GamemodeEntry.players[target].username}</size>").Send(id);

            NetworkManager.broadcastPosition[id] = false;
        }
        public void ExitSpectate()
        {
            new MessageServerToClient.MessageSpectate().Send(id);
            spectating = 0;
            NetworkManager.broadcastPosition[id] = true;
        }

        public void EnableAdmin()
        {
            admin = true;
            username = "<color=#ee4444>(A)</color> <color=#ff9494>" + username + "</color>";
            new MessageServerToClient.MessageChatMessage($"<color=#c56cf5><b>»</b> {username} is now an admin</color>").SendToAll();
        }

        public string GetPbTime()
        {
            if (pb == 0)
                return " <color=yellow>[No PB set]</color>";
            return $" <color=yellow>[{MessageHandlers.FormatTime(pb)}]</color>";
        }

        public void Destroy()
        {
            username = "";
        }
    }
}
