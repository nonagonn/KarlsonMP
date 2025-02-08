using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
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
    }
}
