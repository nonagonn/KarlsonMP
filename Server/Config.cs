using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public static class Config
    {
        public const int TPS = 120; // TPS doesn't reaaaaly matter that much
        // unity runs around ~160 tps, 120 is good enough, because you get throttled anyway
        public const int MSPT = 1000 / TPS;
        public const int PORT = 11337;
        public const int HTTP_PORT = 11338;
    }
}
