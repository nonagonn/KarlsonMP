using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class BulletRenderer
    {
        public static void DrawBullet(Vector3 from, Vector3 to) => DrawBullet(from, to, Color.white);
        public static void DrawBullet(Vector3 from, Vector3 to, Color color)
        {
            UnityEngine.Object.Instantiate(PrefabManager.Instance.bulletDestroy, to, Quaternion.identity);
            UnityEngine.Object.Instantiate(PrefabManager.Instance.bulletHitAudio, to, Quaternion.identity);
            GameObject br = new GameObject("Bullet Renderer");
            LineRenderer lr = br.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.widthMultiplier = 0.2f;
            lr.positionCount = 2;
            lr.startColor = color;
            lr.endColor = color;
            lr.SetPositions(new Vector3[] { from, to });
            IEnumerator DestroyBr()
            {
                yield return new WaitForSeconds(0.1f);
                lr.widthMultiplier = 0.15f;
                yield return new WaitForSeconds(0.1f);
                lr.widthMultiplier = 0.10f;
                yield return new WaitForSeconds(0.1f);
                lr.widthMultiplier = 0.05f;
                yield return new WaitForSeconds(0.1f);
                lr.widthMultiplier = 0.0f;
                yield return new WaitForSeconds(0.1f);
                UnityEngine.Object.Destroy(br);
            }
            Loader.monoHooks.StartCoroutine(DestroyBr());
        }
    }
}
