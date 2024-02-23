using Riptide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP.GamemodeApi
{
    public static class MessageComponents
    {
        public class Optional_Vector2 : IMessageSerializable
        {
            private static readonly Optional_Vector2 _none = new Optional_Vector2 { value = false };
            public static Optional_Vector2 none => _none;
            private bool value = false;
            private Vector2 V;
            public Optional_Vector2(Vector2 v) { V = v; value = true; }
            public Optional_Vector2() { }
            public bool HasValue() => value;
            public Vector2 GetValue()
            {
                if (value)
                    return V;
                return Vector2.zero;
            }
            public void Serialize(Message message)
            {
                message.Add(value);
                if (value)
                    message.Add(V);
            }
            public void Deserialize(Message message)
            {
                value = message.GetBool();
                if (value)
                    V = message.GetVector2();
            }
            public static implicit operator Optional_Vector2(Vector2? value)
            {
                if (!value.HasValue)
                    return none;
                return new Optional_Vector2(value.Value);
            }
        }
        public class Optional_Vector3 : IMessageSerializable
        {
            private static readonly Optional_Vector3 _none = new Optional_Vector3 { value = false };
            public static Optional_Vector3 none => _none;
            private bool value = false;
            private Vector3 V;
            public Optional_Vector3(Vector3 v) { V = v; value = true; }
            public Optional_Vector3() { }
            public bool HasValue() => value;
            public Vector3 GetValue()
            {
                if (value)
                    return V;
                return Vector3.zero;
            }
            public void Serialize(Message message)
            {
                message.Add(value);
                if (value)
                    message.Add(V);
            }
            public void Deserialize(Message message)
            {
                value = message.GetBool();
                if (value)
                    V = message.GetVector3();
            }
            public static implicit operator Optional_Vector3(Vector3? value)
            {
                if (!value.HasValue)
                    return none;
                return new Optional_Vector3(value.Value);
            }
        }
    }

    public static class MessageServerToClient
    {
        public abstract class MessageBase_S2C
        {
            public Message RiptideMessage;
            public MessageBase_S2C Send(ushort client)
            {
                NetworkManager.server.Send(RiptideMessage, client);
                return this;
            }
            public MessageBase_S2C SendToAll()
            {
                NetworkManager.server.SendToAll(RiptideMessage);
                return this;
            }
            public MessageBase_S2C SendToAll(ushort client)
            {
                NetworkManager.server.SendToAll(RiptideMessage, client);
                return this;
            }
        }

        public class MessageInitialPlayerList : MessageBase_S2C
        {
            private List<(ushort, string)> pushList;
            private bool compiled;

            /// <summary>
            /// Construct a packet with the players in the list
            /// </summary>
            /// <param name="players">list of players with (id, username)</param>
            public MessageInitialPlayerList(List<(ushort, string)> players)
            {
                pushList = players;
                Compile();
            }
            /// <summary>
            /// Construct a dynamic packet
            /// Add players with .AddPlayer(id, username)
            /// Before sending, use .Compile()
            /// </summary>
            public MessageInitialPlayerList()
            {
                pushList = new List<(ushort, string)>();
                compiled = false;
            }
            public MessageInitialPlayerList AddPlayer(ushort id, string name)
            {
                pushList.Add((id, name));
                return this;
            }
            public MessageInitialPlayerList Compile()
            {
                if (compiled) return this;
                compiled = true;
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.initialPlayerList);
                RiptideMessage.Add(pushList.Count);
                foreach (var p in pushList)
                    RiptideMessage.Add(p.Item1).Add(p.Item2);
                return this;
            }
        }
        public class MessagePlayerJoinLeave : MessageBase_S2C
        {
            /// <summary>
            /// Player join
            /// </summary>
            /// <param name="id">Player ID</param>
            /// <param name="username">Player Username</param>
            public MessagePlayerJoinLeave(ushort id, ushort username)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.addPlayer);
                RiptideMessage.Add(true).Add(id).Add(username);
            }
            /// <summary>
            /// Player leave
            /// </summary>
            /// <param name="id">Player ID</param>
            public MessagePlayerJoinLeave(ushort id)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.addPlayer);
                RiptideMessage.Add(false).Add(id);
            }
        }
        public class MessagePositionData : MessageBase_S2C
        {
            public MessagePositionData(MessageClientToServer.MessagePositionData clientMessage) : this(clientMessage.position, clientMessage.rotation, clientMessage.crouching, clientMessage.moving, clientMessage.grounded) { }
            public MessagePositionData(Vector3 position, Vector2 rotation, bool crouching, bool moving, bool grounded)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.playerData);
                RiptideMessage.Add(position).Add(rotation).Add(crouching).Add(moving).Add(grounded);
            }
        }
        public class MessageSendBullet : MessageBase_S2C
        {
            public MessageSendBullet(Vector3 from, Vector3 to, Vector3 bulletColor)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.bullet);
                RiptideMessage.Add(from).Add(to).Add(bulletColor);
            }
        }
        public class MessageKillFeed : MessageBase_S2C
        {
            public MessageKillFeed(string message)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.killFeed);
                RiptideMessage.Add(message);
            }
        }
        public class MessageTeleport : MessageBase_S2C
        {
            public MessageTeleport(MessageComponents.Optional_Vector3 position, MessageComponents.Optional_Vector2 rotation, MessageComponents.Optional_Vector3 velocity)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.teleport);
                RiptideMessage.Add(position).Add(rotation).Add(position);
            }
        }
        public class MessageMapChange : MessageBase_S2C
        {
            /// <summary>
            /// Tell client to change to vanilla scene
            /// </summary>
            /// <param name="sceneName">scene name</param>
            public MessageMapChange(string sceneName)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.map);
                RiptideMessage.Add(true).Add(sceneName);
            }
            /// <summary>
            /// Tell client to change to custom map
            /// </summary>
            /// <param name="mapName">Map Name</param>
            /// <param name="HTTP_PORT">port to use when connecting to http mapdownloader server</param>
            public MessageMapChange(string mapName, ushort HTTP_PORT)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.map);
                RiptideMessage.Add(false).Add(mapName).Add(HTTP_PORT);
            }
        }
        public class MessageSetHP : MessageBase_S2C
        {
            public MessageSetHP(int hp)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.hp);
                RiptideMessage.Add(hp);
            }
        }
        public class MessageConfirmKill : MessageBase_S2C
        {
            public MessageConfirmKill(ushort victim)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.kill);
                RiptideMessage.Add(victim);
            }
        }
        public class MessageDied : MessageBase_S2C
        {
            public MessageDied(ushort killer)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.death);
                RiptideMessage.Add(killer);
            }
        }
        public class MessageRespawn : MessageBase_S2C
        {
            public MessageRespawn(Vector3 position)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.respawn);
                RiptideMessage.Add(position);
            }
        }
        public class MessageChatMessage : MessageBase_S2C
        {
            public MessageChatMessage(string message)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.chat);
                RiptideMessage.Add(message);
            }
        }
        public class MessageUpdateScoreboard : MessageBase_S2C
        {
            private List<(ushort, string, int, int, int)> pushList;
            private bool compiled;

            public MessageUpdateScoreboard(List<(ushort, string, int, int, int)> entries)
            {
                pushList = entries;
                Compile();
            }
            public MessageUpdateScoreboard()
            {
                pushList = new List<(ushort, string, int, int, int)>();
                compiled = false;
            }
            public MessageUpdateScoreboard AddEntry(ushort id, string name, int kills, int deaths, int score)
            {
                pushList.Add((id, name, kills, deaths, score));
                return this;
            }
            public MessageUpdateScoreboard Compile()
            {
                if (compiled) return this;
                compiled = true;
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.scoreboard);
                RiptideMessage.Add(pushList.Count);
                foreach (var p in pushList)
                    RiptideMessage.Add(p.Item1).Add(p.Item2).Add(p.Item3).Add(p.Item4).Add(p.Item5);
                return this;
            }
        }
        public class MessageColorPlayer : MessageBase_S2C
        {
            public MessageColorPlayer(ushort who, string color)
            {
                if (color != "yellow" && color != "red" && color != "blue")
                    color = "yellow";
                Message message = Message.Create(MessageSendMode.Reliable, Packet_S2C.colorPlayer);
                message.Add(who).Add(color);
            }
        }
        public class MessageSpectate : MessageBase_S2C
        {
            /// <summary>
            /// Start spectating
            /// </summary>
            /// <param name="target">Player ID of target we want to spectate</param>
            public MessageSpectate(ushort target)
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.spectate);
                RiptideMessage.Add(true).Add(target);
            }
            /// <summary>
            /// Stop spectating
            /// </summary>
            public MessageSpectate()
            {
                RiptideMessage = Message.Create(MessageSendMode.Reliable, Packet_S2C.spectate);
                RiptideMessage.Add(false);
            }
        }
    }

    public static class MessageClientToServer
    {
        public abstract class MessageBase_C2S
        {
            public ushort fromId;
            public ushort RiptideId;
            public Message RiptideMessage;
            public MessageBase_C2S(ushort fromId, ushort riptideId, Message riptideMessage)
            {
                this.fromId = fromId;
                RiptideId = riptideId;
                RiptideMessage = riptideMessage;
            }
        }

        public class MessageHandshake : MessageBase_C2S
        {
            public string username;
            public MessageHandshake(ushort fromId, Message riptideMessage) : base(fromId, Packet_C2S.handshake, riptideMessage)
            {
                username = RiptideMessage.GetString();
            }
        }
        public class MessagePositionData : MessageBase_C2S
        {
            public Vector3 position;
            public Vector2 rotation;
            public bool crouching, moving, grounded;
            public MessagePositionData(ushort fromId, Message riptideMessage) : base(fromId, Packet_C2S.position, riptideMessage)
            {
                position = RiptideMessage.GetVector3();
                rotation = RiptideMessage.GetVector2();
                crouching = RiptideMessage.GetBool();
                moving = RiptideMessage.GetBool();
                grounded = RiptideMessage.GetBool();
            }
        }
        public class MessageRequestScene : MessageBase_C2S
        {
            public MessageRequestScene(ushort fromId, Message riptideMessage) : base(fromId, Packet_C2S.requestScene, riptideMessage) { }
        }
        public class MessageShoot : MessageBase_C2S
        {
            public Vector3 origin;
            public Vector3 hitPoint;

            public MessageShoot(ushort fromId, Message riptideMessage) : base(fromId, Packet_C2S.shoot, riptideMessage)
            {
                origin = RiptideMessage.GetVector3();
                hitPoint = RiptideMessage.GetVector3();
            }
        }
        public class MessageDamage : MessageBase_C2S
        {
            public ushort victim;
            public int damage;

            public MessageDamage(ushort fromId, Message riptideMessage) : base(fromId, Packet_C2S.damage, riptideMessage)
            {
                victim = RiptideMessage.GetUShort();
                damage = RiptideMessage.GetInt();
            }
        }
        public class MessageChat : MessageBase_C2S
        {
            public string message;

            public MessageChat(ushort fromId, Message riptideMessage) : base(fromId, Packet_C2S.chat, riptideMessage)
            {
                message = riptideMessage.GetString();
            }
        }
    }
}
