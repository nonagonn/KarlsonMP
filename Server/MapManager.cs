using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerKMP
{
    public static class MapManager
    {
        public static List<Map> defaultMaps = new List<Map>();

        public static void Init()
        {
            defaultMaps.Add(new Map("1Sandbox0", new List<(string, Vector3, float)>()
            {
                ("default", new Vector3(0f, 0f, -50f), 0f)
            }, true));
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Maps"));
            currentMap = defaultMaps[0];
        }

        public static Map currentMap = null;

        public static void LoadMap(string mapName)
        {
            if(defaultMaps.Count(x => x.name == mapName) == 1)
            {
                currentMap = (from x in defaultMaps where x.name == mapName select x).First();
                Console.WriteLine("Switched map to default map " + mapName);
            }
            else
            {
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Maps", mapName + ".kme_raw")))
                    Console.WriteLine($"File '{mapName}.kme_raw' doesn't exist!");
                else if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Maps", mapName + ".kme_data")))
                    Console.WriteLine($"File '{mapName}.kme_data' doesn't exist!");
                else
                {
                    List<(string, Vector3, float)> spawnPos = new List<(string, Vector3, float)>();
                    using(FileStream fs = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "Maps", mapName + ".kme_data")))
                    using(BinaryReader br = new BinaryReader(fs))
                    {
                        int len = br.ReadInt32();
                        while(len-- > 0)
                            spawnPos.Add((br.ReadString(), new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()), br.ReadSingle()));
                    }
                    Map map = new Map(mapName, spawnPos, false);
                    currentMap = map;
                    Console.WriteLine("Switched map to custom map " + mapName);
                    MapDownloader.mapData = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Maps", mapName + ".kme_raw"));
                }
            }
            ServerSend.MapChange();
        }

        public class Map
        {
            public string name;
            public List<(string, Vector3, float)> spawnPositions;
            public bool isDefault = false;
            public Map(string name, List<(string, Vector3, float)> spawnPositions, bool isDefault)
            {
                this.name = name;
                this.spawnPositions = spawnPositions;
                this.isDefault = isDefault;
            }
        }
    }
}
