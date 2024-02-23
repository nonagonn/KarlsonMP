using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP.GamemodeApi
{
    public abstract class Gamemode
    {
        public virtual void ProcessMessage(MessageClientToServer.MessageBase_C2S message) { }
        public virtual void OnPlayerDisconnect(ushort id) { }
        public virtual void OnStart() { }
        public virtual void OnStop() { }
        public virtual void ServerTick() { }
        public virtual void OnMapChange() { }
    }
}
