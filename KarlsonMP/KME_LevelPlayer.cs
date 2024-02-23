using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Xml.Linq;

namespace KarlsonMP
{
    public static class KME_LevelPlayer
    {
        public static Texture2D[] gameTex { get; private set; } = null;

        public static void InitGameTex()
        {
            List<Texture2D> temp = new List<Texture2D>();
            foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                switch (t.name)
                {
                    default: break;
                    case "GridBox_Default":
                    case "prototype_512x512_grey3":
                    case "prototype_512x512_white":
                    case "prototype_512x512_yellow":
                    case "Floor":
                    case "Blue":
                    case "Red":
                    case "Barrel":
                    case "Orange":
                    case "Yellow":
                    case "UnityWhite":
                    case "UnityNormalMap":
                    case "Sunny_01B_down":
                        if (temp.Count(x => x.name == t.name) == 0)
                            temp.Add(t);
                        break;
                }
            }
            gameTex = temp.ToArray();
            if (gameTex.Length != 13) KMP_Console.Log("<color=red>Invalid game texture array. Expected 13 items, got " + gameTex.Length + "</color>");
            foreach (var t in gameTex)
            {
                KMP_Console.Log(t.name);
            }
        }

        public static string currentLevel { get; private set; } = "";
        public static void ExitedLevel() => currentLevel = "";
        private static LevelData levelData;

        public static void LoadLevel(string name, byte[] data)
        {
            if (gameTex == null)
                InitGameTex();
            currentLevel = name;
            levelData = new LevelData(data);
            SceneManager.sceneLoaded += LoadLevelData;
            SceneManager.LoadScene("4Escape0");
            Game.Instance.StartGame();
        }

        public static void LoadLevelData(Scene arg0, LoadSceneMode arg1)
        {
            foreach (Collider c in UnityEngine.Object.FindObjectsOfType<Collider>())
                if (c.gameObject != PlayerMovement.Instance.gameObject && c.gameObject.GetComponent<DetectWeapons>() == null) UnityEngine.Object.Destroy(c.gameObject);

            PlayerMovement.Instance.transform.position = levelData.startPosition;
            PlayerMovement.Instance.playerCam.transform.localRotation = Quaternion.Euler(0f, levelData.startOrientation, 0f);
            PlayerMovement.Instance.orientation.transform.localRotation = Quaternion.Euler(0f, levelData.startOrientation, 0f);

            foreach (var obj in levelData.Objects)
            {
                if (obj.IsPrefab) continue;
                GameObject go;
                if (obj.Glass)
                {
                    go = KMP_PrefabManager.NewGlass();
                    if (obj.DisableTrigger)
                    {
                        // fix collider
                        go.GetComponent<BoxCollider>().isTrigger = false;
                        go.GetComponent<BoxCollider>().size = Vector3.one;
                        UnityEngine.Object.Destroy(go.GetComponent<Glass>());
                    }
                }
                else if (obj.Lava)
                {
                    go = KMP_PrefabManager.NewGlass();
                    UnityEngine.Object.Destroy(go.GetComponent<Glass>());
                    go.AddComponent<Lava>();
                }
                else
                    go = KMP_PrefabManager.NewCube();
                if (obj.MarkAsObject)
                    // set layer to object so you can't wallrun / grapple
                    go.layer = LayerMask.NameToLayer("Object");
                if (obj.TextureId < gameTex.Length)
                    go.GetComponent<MeshRenderer>().material.mainTexture = gameTex[obj.TextureId];
                else
                    go.GetComponent<MeshRenderer>().material.mainTexture = levelData.Textures[obj.TextureId - gameTex.Length];
                go.GetComponent<MeshRenderer>().material.color = obj._Color;
                if (obj.Bounce)
                    go.GetComponent<BoxCollider>().material = KMP_PrefabManager.BounceMaterial();
                go.transform.position = obj.Position;
                go.transform.rotation = Quaternion.Euler(obj.Rotation);
                go.transform.localScale = obj.Scale;
            }

            SceneManager.sceneLoaded -= LoadLevelData;
            GameObject.Find("Managers (1)/UI/Game/Timer").SetActive(false);
            ClientSend.RequestScene();
        }

        public class LevelData
        {
            public static readonly PrimitiveType[] typeToInt = new PrimitiveType[] { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule, PrimitiveType.Cylinder, PrimitiveType.Plane, PrimitiveType.Quad };

            public LevelData(byte[] _data)
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(_data)))
                {
                    int version = br.ReadInt32();
                    KMP_Console.Log("Loading level version " + version);
                    if (version == 1)
                        LoadLevel_Version1(br);
                    else if (version == 2)
                    {
                        gridAlign = br.ReadSingle();
                        startingGun = br.ReadInt32();
                        startPosition = br.ReadVector3();
                        startOrientation = br.ReadSingle();
                        int _len;
                        List<Texture2D> list = new List<Texture2D>();
                        int _texl = br.ReadInt32();
                        while (_texl-- > 0)
                        {
                            string _name = br.ReadString();
                            _len = br.ReadInt32();
                            list.Add(new Texture2D(1, 1));
                            list.Last().LoadImage(br.ReadBytes(_len));
                            list.Last().name = _name;
                        }
                        Textures = list.ToArray();
                        List<LevelObject> objects = new List<LevelObject>();
                        _texl = br.ReadInt32();
                        while (_texl-- > 0)
                        {
                            bool prefab = br.ReadBoolean();
                            string name = br.ReadString();
                            string group = br.ReadString();
                            if (prefab)
                                objects.Add(new LevelObject(br.ReadInt32(), br.ReadVector3(), br.ReadVector3(), br.ReadVector3(), name, group, br.ReadInt32()));
                            else
                                objects.Add(new LevelObject(br.ReadVector3(), br.ReadVector3(), br.ReadVector3(), br.ReadInt32(), br.ReadColor(), name, group, br.ReadBoolean(), br.ReadBoolean(), br.ReadBoolean(), br.ReadBoolean(), br.ReadBoolean()));
                        }
                        Objects = objects.ToArray();
                    }
                    else
                    {
                        KMP_Console.Log("<color=red>Unknown level version " + version + "</color>");
                        KMP_Console.Log("Try to update KME to the latest version.");
                    }
                }
            }

            private void LoadLevel_Version1(BinaryReader br)
            {
                gridAlign = br.ReadSingle();
                startingGun = br.ReadInt32();
                startPosition = br.ReadVector3();
                startOrientation = br.ReadSingle();
                int _len;
                List<Texture2D> list = new List<Texture2D>();
                int _texl = br.ReadInt32();
                while (_texl-- > 0)
                {
                    string _name = br.ReadString();
                    _len = br.ReadInt32();
                    list.Add(new Texture2D(1, 1));
                    list.Last().LoadImage(br.ReadBytes(_len));
                    list.Last().name = _name;
                }
                Textures = list.ToArray();
                List<LevelObject> objects = new List<LevelObject>();
                _texl = br.ReadInt32();
                while (_texl-- > 0)
                {
                    bool prefab = br.ReadBoolean();
                    string name = br.ReadString();
                    string group = br.ReadString();
                    if (prefab)
                        objects.Add(new LevelObject(br.ReadInt32(), br.ReadVector3(), br.ReadVector3(), br.ReadVector3(), name, group, br.ReadInt32()));
                    else
                        objects.Add(new LevelObject(br.ReadVector3(), br.ReadVector3(), br.ReadVector3(), br.ReadInt32(), br.ReadColor(), name, group, br.ReadBoolean(), br.ReadBoolean(), br.ReadBoolean(), br.ReadBoolean(), false));
                }
                Objects = objects.ToArray();
            }

            public float gridAlign;
            public int startingGun;
            public Vector3 startPosition;
            public float startOrientation;

            public Texture2D[] Textures;
            public LevelObject[] Objects;

            public class LevelObject
            {
                public LevelObject(int prefabId, Vector3 position, Vector3 rotation, Vector3 scale, string name, string groupName, int prefabData)
                {
                    IsPrefab = true;
                    PrefabId = prefabId;

                    Position = position;
                    Rotation = rotation;
                    Scale = scale;

                    Name = name;
                    GroupName = groupName;

                    PrefabData = prefabData;
                }
                public LevelObject(Vector3 position, Vector3 rotation, Vector3 scale, int textureId, Color color, string name, string groupName, bool bounce, bool glass, bool lava, bool disableTrigger, bool markAsObject)
                {
                    IsPrefab = false;
                    TextureId = textureId;

                    Position = position;
                    Rotation = rotation;
                    Scale = scale;
                    _Color = color;

                    Name = name;
                    GroupName = groupName;
                    Bounce = bounce;
                    Glass = glass;
                    Lava = lava;
                    DisableTrigger = disableTrigger;
                    MarkAsObject = markAsObject;
                }

                public bool IsPrefab;
                public Vector3 Position;
                public Vector3 Rotation;
                public Vector3 Scale;
                public Color _Color;
                public string Name;
                public string GroupName;

                public int PrefabId;

                public int TextureId;
                public bool Bounce;
                public bool Glass;
                public bool Lava;
                public bool DisableTrigger;
                public bool MarkAsObject;

                public int PrefabData;

                public override string ToString()
                {
                    string st = "(PF:" + IsPrefab;
                    if (IsPrefab)
                        st += " " + PrefabId;
                    st += " " + Position + " " + Rotation + " " + Scale;
                    if (!IsPrefab)
                        st += " tex:" + TextureId;
                    st += ")";
                    return st;
                }
            }
        }
    }
}
