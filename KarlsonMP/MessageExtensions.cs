using Riptide;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public static class MessageExtensions
    {
        public static Message Add(this Message message, Vector2 value) => AddVector2(message, value);
        public static Message AddVector2(this Message message, Vector2 value) => message.AddFloat(value.x).AddFloat(value.y);
        public static Vector2 GetVector2(this Message message) => new Vector2(message.GetFloat(), message.GetFloat());

        public static Message Add(this Message message, Vector3 value) => AddVector3(message, value);
        public static Message AddVector3(this Message message, Vector3 value) => message.AddFloat(value.x).AddFloat(value.y).AddFloat(value.z);
        public static Vector3 GetVector3(this Message message) => new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());


        public class oVector2 : IMessageSerializable
        {
            private static readonly oVector2 _none = new oVector2 { value = false };
            public static oVector2 none => _none;
            private bool value = false;
            private Vector2 V;
            public oVector2(Vector2 v) { V = v; value = true; }
            public oVector2() { }
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
            public static implicit operator oVector2(Vector2? value)
            {
                if (!value.HasValue)
                    return none;
                return new oVector2(value.Value);
            }
        }
        public class oVector3 : IMessageSerializable
        {
            private static readonly oVector3 _none = new oVector3 { value = false };
            public static oVector3 none => _none;
            private bool value = false;
            private Vector3 V;
            public oVector3(Vector3 v) { V = v; value = true; }
            public oVector3() { }
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
            public static implicit operator oVector3(Vector3? value)
            {
                if (!value.HasValue)
                    return none;
                return new oVector3(value.Value);
            }
        }
    }

    public static class BinaryExtensions
    {
        public static Vector3 ReadVector3(this BinaryReader br)
        {
            float f1 = br.ReadSingle();
            float f2 = br.ReadSingle();
            float f3 = br.ReadSingle();
            return new Vector3(f1, f2, f3);
        }

        public static void Write(this BinaryWriter bw, Vector3 v)
        {
            bw.Write(v.x);
            bw.Write(v.y);
            bw.Write(v.z);
        }

        public static Color ReadColor(this BinaryReader br)
        {
            float f1 = br.ReadSingle();
            float f2 = br.ReadSingle();
            float f3 = br.ReadSingle();
            float f4 = br.ReadSingle();
            return new Color(f1, f2, f3, f4);
        }

        public static void Write(this BinaryWriter bw, Color c)
        {
            bw.Write(c.r);
            bw.Write(c.g);
            bw.Write(c.b);
            bw.Write(c.a);
        }
    }
}
