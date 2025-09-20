using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        private static readonly Vector2 zeroVector = new Vector2(0f, 0f);
        public static Vector2 zero => zeroVector;
    }
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        private static readonly Vector3 zeroVector = new Vector3(0f, 0f, 0f);
        public static Vector3 zero => zeroVector;

        public static Vector3 Parse(string s)
        {
            var split = s.Split(',');
            return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
        }

        public static Vector3 operator +(Vector3 lhs, Vector3 rhs) => new(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        public static Vector3 operator -(Vector3 lhs, Vector3 rhs) => new(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        public static Vector3 operator *(Vector3 lhs, int rhs) => new(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
        public static Vector3 operator /(Vector3 lhs, int rhs) => new(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
    }
}
