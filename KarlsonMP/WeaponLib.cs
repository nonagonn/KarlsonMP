using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public static class WeaponLib
    {
        //new Vector3(1.44f, 0.527f, 1.44f)
        public static GameObject MakeGun(Mesh mesh, Material[] materials, Vector3 localScale, Vector3 meshRotation, Vector3 gunTip, float recoil, float attackSpeed)
        {
            GameObject pistol = KMP_PrefabManager.NewPistol();
            pistol.GetComponent<MeshFilter>().mesh = mesh;
            pistol.GetComponent<MeshRenderer>().materials = materials;
            pistol.transform.position = PlayerMovement.Instance.transform.position;
            pistol.transform.localScale = localScale;
            var rw = pistol.GetComponent<RangedWeapon>();
            rw.recoil = recoil;
            rw.attackSpeed = attackSpeed;
            rw.accuracy = 0f;
            pistol.transform.GetChild(0).localPosition = gunTip;

            // rotate mesh
            var quat = Quaternion.Euler(meshRotation);
            var msh = pistol.GetComponent<MeshFilter>().mesh;
            Vector3[] vert = new Vector3[msh.vertexCount];
            Array.Copy(msh.vertices, vert, vert.Length);
            for (int i = 0; i < vert.Length; i++)
                vert[i] = quat * vert[i];
            msh.vertices = vert;
            msh.UploadMeshData(true); // free up system memory

            return pistol;
        }
    }
}
