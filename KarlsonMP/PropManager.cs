using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class PropManager
    {
        public static void SpawnProp(int id, Vector3 pos, Vector3 rot, Vector3 scale, int prefabId, bool annouce_pickup)
        {
            GameObject go = PrefabIdToGameObject(prefabId);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(rot);
            go.transform.localScale = scale;
            Prop.CreateProp(id, go, annouce_pickup);
        }

        public static void LinkPropToPlayer(int id, ushort playerId, Vector3 pos, Vector3 rot)
        {
            activeProps[id].playerid = playerId;
            activeProps[id].posOff = pos;
            activeProps[id].rotOff = rot;
        }

        public static void DestroyProp(int id)
        {
            UnityEngine.Object.Destroy(activeProps[id].go);
            activeProps.Remove(id);
        }

        public static void _onupdate()
        {

        }

        private static GameObject PrefabIdToGameObject(int id)
        {
            if (id == 0) return KMP_PrefabManager.NewMilk();
            return new GameObject();
        }

        private static Dictionary<int, Prop> activeProps = new Dictionary<int, Prop>();
        public class Prop
        {
            public int id;
            public GameObject go;
            public ushort playerid;
            public Vector3 posOff;
            public Vector3 rotOff;

            private Prop(int id, GameObject go, bool annouce)
            {
                this.id = id;
                this.go = go;
                playerid = 0;
                var prop_data = go.AddComponent<KMP_PropData>();
                prop_data.id = id;
                prop_data.annouce = annouce;
            }

            public static void CreateProp(int id, GameObject go, bool annouce = false)
            {
                activeProps.Add(id, new Prop(id, go, annouce));
            }
        }
        public class KMP_PropData : MonoBehaviour
        {
            public int id;
            public bool annouce;
        }
    }
}
