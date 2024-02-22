using Audio;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace KarlsonMP
{
    public class Inventory
    {
        public static void LoadAssets(AssetBundle bundle)
        {
            deagle = bundle.LoadAsset<GameObject>("assets/karlsonmp/models/deag.fbx");
            ak47 = bundle.LoadAsset<GameObject>("assets/karlsonmp/models/ak.fbx");
        }

        private static GameObject deagle, ak47;

        public static void Shoot(Transform ___guntip)
        {
            if (CurrentWeaponCooldown > 0) return; // weapon switch use cooldown
            if (ReloadTime[CurrentWeapon] > 0) return; // currently reloading
            if (WeaponMag[CurrentWeapon] > 0)
            {
                // virtual shoot
                Gun.Instance.Shoot();
                if (CurrentWeapon == 0)
                    KMP_AudioManager.PlaySound("smg", 0.15f);
                if (CurrentWeapon == 1)
                    KMP_AudioManager.PlaySound("pistol", 0.15f);
                if (CurrentWeapon == 2)
                    KMP_AudioManager.PlaySound("shotgun", 0.15f);
                // atm i don't know more abt this.. will need to reverse this code a bit more
                Vector3 vector = ___guntip.position - ___guntip.transform.right / 4f;
                UnityEngine.Object.Instantiate(PrefabManager.Instance.muzzle, vector, Quaternion.identity);
                Dictionary<ushort, float> damage = new Dictionary<ushort, float>();
                ushort victim;
                float dmg;
                for (int _ = 0; _ < BulletCount[CurrentWeapon]; _++)
                {
                    (victim, dmg) = TraceBullet(___guntip);
                    if (dmg == 0f) continue;
                    if(!damage.ContainsKey(victim))
                        damage.Add(victim, 0f);
                    damage[victim] += dmg;
                }
                    
                WeaponMag[CurrentWeapon]--;

                if (damage.Count > 0)
                    Hitmarker();

                // send damage report
                foreach(var x in damage)
                    ClientSend.Damage(x.Key, (int)Mathf.Floor(x.Value + 0.5f));
            }
            if (WeaponMag[CurrentWeapon] == 0)
            {
                ReloadTime[CurrentWeapon] = MaxReloadTime;
                // 0.1s delay, to enjoy deagle sound
                KMP_AudioManager.PlaySoundDelayed("reload", 0.10f, 0.08f);
            }
        }

        private static void Hitmarker()
        {
            KMP_AudioManager.PlaySound("hitmarker", 0.15f);
            GameObject crosshair = GameObject.Find("Managers (1)/UI/Game/Crosshair");
            GameObject hitmarker = UnityEngine.Object.Instantiate(crosshair);
            hitmarker.transform.SetParent(crosshair.transform.parent);
            hitmarker.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            hitmarker.transform.localPosition = Vector3.zero;
            hitmarker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            System.Collections.IEnumerator fadeHitmarker()
            {
                var imgs = hitmarker.GetComponentsInChildren<Image>();
                for (int i = 1; i <= 20; ++i)
                {
                    yield return new WaitForSeconds(0.01f);
                    foreach (var x in imgs)
                        x.color = new Color(1f, 1f, 1f, 1f - i / 20f);
                }
                UnityEngine.Object.Destroy(hitmarker);
            }
            Loader.monoHooks.StartCoroutine(fadeHitmarker());
        }

        private static (ushort,float) TraceBullet(Transform ___guntip)
        {
            ushort victim = 0;
            float damage = 0f;
            PlayerMovement.Instance.GetRb().AddForce(WeaponObjects[CurrentWeapon].transform.right * BoostRecoil[CurrentWeapon], ForceMode.Impulse);
            // cast ray
            Vector3 gunTip = PlayerMovement.Instance.transform.Find("Head").position;
            // Player Collider, Ground, Object, Player Capsule Collider
            LayerMask hittable = (1 << 12) | (1 << 9) | (1 << 10) | (1 << 8);
            PlayerMovement.Instance.gameObject.layer = 13; // change layer to not hit our capsule
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.rotation * Vector3.forward + new Vector3(UnityEngine.Random.Range(-WeaponSpread[CurrentWeapon], WeaponSpread[CurrentWeapon]), UnityEngine.Random.Range(-WeaponSpread[CurrentWeapon], WeaponSpread[CurrentWeapon]), UnityEngine.Random.Range(-WeaponSpread[CurrentWeapon], WeaponSpread[CurrentWeapon])), out RaycastHit hitInfo, 200f, hittable))
            {
                BulletRenderer.DrawBullet(___guntip.position, hitInfo.point, Color.blue);
                ClientSend.Shoot(gunTip, hitInfo.point);
                if (hitInfo.transform.gameObject.GetComponent<GameObjectToPlayerId>() != null)
                {
                    float distance = hitInfo.distance;
                    if (DamageDropoff[CurrentWeapon] == 0 || distance <= DamageDropoff[CurrentWeapon])
                    {
                        // calculate damage
                        damage = MaxDamage[CurrentWeapon] - DamageScaleByDist[CurrentWeapon] * distance;
                        KMP_Console.Log($"dist: {distance}. {MaxDamage[CurrentWeapon]} -> {damage}");
                        if (damage < 0) damage = 0f; // dropoff not acounted by weapon specs
                        victim = hitInfo.transform.gameObject.GetComponent<GameObjectToPlayerId>().id;
                    }
                }
            }
            PlayerMovement.Instance.gameObject.layer = 8;
            return (victim, damage);
        }

        public static void Init()
        {
            WeaponMag = new int[] { MaxWeaponMag[0], MaxWeaponMag[1], MaxWeaponMag[2] };
            ReloadTime = new float[] { 0f, 0f, 0f };
            CurrentWeapon = 0;
            WeaponObjects = new GameObject[3];
            WeaponObjects[0] = WeaponLib.MakeGun(ak47.GetComponent<MeshFilter>().mesh, ak47.GetComponent<MeshRenderer>().materials, new Vector3(50f, 50f, 2.5f), Vector3.zero, new Vector3(0f, 180f, 0f), Vector3.zero, 0.2f, 0.15f);
            WeaponObjects[1] = WeaponLib.MakeGun(deagle.GetComponent<MeshFilter>().mesh, deagle.GetComponent<MeshRenderer>().materials, new Vector3(1.44f, 1.44f, 0.527f), Vector3.zero, new Vector3(-90f, 0f, 0f), Vector3.zero, 1f, 0.4f);
            WeaponObjects[1].SetActive(false);
            WeaponObjects[2] = KMP_PrefabManager.NewShotgun();
            WeaponObjects[2].SetActive(false);
            PlayerMovement.Instance.ReflectionGet<DetectWeapons>("detectWeapons").ForcePickup(WeaponObjects[0]);
        }

        public static void SwitchWeapon(int idx)
        {
            KMP_AudioManager.PlaySound("reload", 0.08f);
            Pickup pk = WeaponObjects[CurrentWeapon].GetComponent<Pickup>();
            pk.StopUse();
            WeaponObjects[CurrentWeapon].transform.parent = null;
            WeaponObjects[CurrentWeapon].layer = LayerMask.NameToLayer("Gun");
            pk.readyToUse = true;
            pk.pickedUp = false;
            RangedWeapon rw = WeaponObjects[CurrentWeapon].GetComponent<RangedWeapon>();
            rw.CancelInvoke(); // cancel all active invokes, there is a bug here, produced by our system
            WeaponObjects[CurrentWeapon].SetActive(false);

            if (ReloadTime[CurrentWeapon] > 0)
                ReloadTime[CurrentWeapon] = 0; // we were reloading, so cancel

            CurrentWeapon = idx;
            WeaponObjects[idx].SetActive(true);
            PlayerMovement.Instance.ReflectionGet<DetectWeapons>("detectWeapons").ForcePickup(WeaponObjects[idx]);
            // somewhat nice animation
            WeaponObjects[idx].transform.localPosition = PlayerMovement.Instance.ReflectionGet<DetectWeapons>("detectWeapons").ReflectionGet<Vector3>("desiredPos");
            WeaponObjects[idx].transform.localRotation = Quaternion.Euler(0f, 90f, 179f);
            // switch cooldown
            CurrentWeaponCooldown = MaxCooldownTime[idx];
            WeaponObjects[idx].GetComponent<Pickup>().StopUse();
        }
        public static void NextWeapon()
        {
            int idx = CurrentWeapon + 1;
            if (idx > 2) idx = 0;
            SwitchWeapon(idx);
        }
        public static void PrevWeapon()
        {
            int idx = CurrentWeapon - 1;
            if (idx < 0) idx = 2;
            SwitchWeapon(idx);
        }

        public static void ReloadAll()
        {
            WeaponMag = new int[] { MaxWeaponMag[0], MaxWeaponMag[1], MaxWeaponMag[2] };
            ReloadTime = new float[] { 0f, 0f, 0f };
        }

        // 0 - ak47, 1 - deagle, 2 - shotgun
        private static int CurrentWeapon = 0;
        private static int[] WeaponMag = new int[] { 30, 1, 8 };
        private static float[] ReloadTime = new float[] { 0f, 0f, 0f };
        private static float CurrentWeaponCooldown = 0f;

        public static bool CanShoot => CurrentWeaponCooldown == 0f && ReloadTime[CurrentWeapon] == 0f;

        // weapon stats
        private static readonly int[]
            MaxWeaponMag = new int[] { 30, 1, 8 }, // max bullets in magazine
            BulletCount = new int[] { 1, 1, 6 }; // bullets in a shot
        private static readonly float[]
            WeaponSpread = new float[] { 0.03f, 0f, 0.1f }, // spread of a bullet
            MaxCooldownTime = new float[] { 0.2f, 0.7f, 0.5f }, // cooldown time (after reload/swtich)
            BoostRecoil = new float[] { 0f, 0f, 7f }, // boost back (shotgun effect) per bullet
            MaxDamage = new float[] { 20f, 100f, 20f }, // maximum damage the a bullet can deal (when target is at 0f distance or DamageDropoff is 0)
            DamageDropoff = new float[] { 0f, 0f, 50f }, // distance after which damage is 0
            DamageScaleByDist = new float[] { 0.1f, 0f, 0.3f }; // rate at which damage drops off with distance
                                                                // if dist < damagedropoff : damage = MaxDamage - DamageScaleByDist * distance
                                                                // idealy DamageDropoff should be MaxDamage / DamageScaleByDist
                                                                // but it doesn't have to be
        private static readonly float MaxReloadTime = 0.5f;
        private static GameObject[] WeaponObjects;

        public static void Update()
        {
            if(Input.GetKeyDown(KeyCode.R))
                if(ReloadTime[CurrentWeapon] == 0 && WeaponMag[CurrentWeapon] < MaxWeaponMag[CurrentWeapon]) // manual reload
                {
                    ReloadTime[CurrentWeapon] = MaxReloadTime;
                    KMP_AudioManager.PlaySound("reload", 0.08f);
                }

            if (CurrentWeaponCooldown != 0)
            {
                CurrentWeaponCooldown -= Time.deltaTime;
                if (CurrentWeaponCooldown <= 0f)
                    CurrentWeaponCooldown = 0f;
            }
            else
            {
                if (ReloadTime[CurrentWeapon] != 0)
                {
                    ReloadTime[CurrentWeapon] -= Time.deltaTime;
                    if (ReloadTime[CurrentWeapon] <= 0f)
                    {
                        // finished reloading
                        ReloadTime[CurrentWeapon] = 0f;
                        WeaponMag[CurrentWeapon] = MaxWeaponMag[CurrentWeapon];
                        CurrentWeaponCooldown = MaxCooldownTime[CurrentWeapon];
                    }
                    else
                    {
                        // animation for reloading
                        // cubic ease-out
                        float zAngle = 360f * Mathf.Pow(ReloadTime[CurrentWeapon] / MaxReloadTime, 3);
                        WeaponObjects[CurrentWeapon].transform.localRotation = Quaternion.Euler(0f, 90f, zAngle);
                    }
                }
            }
        }

        private static Texture2D ammoIcon;
        public static void GuiCtor(AssetBundle bundle)
        {
            ammoIcon = bundle.LoadAsset<Texture2D>("assets/karlsonmp/ammo_icon.png");
            KMP_Console.Log(ammoIcon.name);
        }

        public static void OnGUI()
        {
            GUI.DrawTexture(new Rect(Screen.width - 100f, Screen.height - 100f, 50f, 50f), ammoIcon);
            GUI.Label(new Rect(Screen.width - 220f, Screen.height - 100f, 140f, 50f), $"<b><size=50><color=white>{WeaponMag[CurrentWeapon]} </color></size><size=25><color=silver>/{MaxWeaponMag[CurrentWeapon]}</color></size></b>");
        }
    }


    [HarmonyPatch(typeof(PlayerMovement), "Start")]
    public class Hook_PlayerMovement_Start
    {
        public static void Prefix(PlayerMovement __instance)
        {
            __instance.spawnWeapon = null;
        }
        public static void Postfix()
        {
            Inventory.Init();
        }
    }

    [HarmonyPatch(typeof(DetectWeapons), "Pickup")]
    public static class Hook_DetectWeapons_Pickup
    {
        public static bool Prefix()
        {
            Inventory.NextWeapon();
            return false;
        }
    }
    [HarmonyPatch(typeof(DetectWeapons), "Throw")]
    public static class Hook_DetectWeapons_Throw
    {
        public static bool Prefix()
        {
            Inventory.PrevWeapon();
            return false;
        }
    }
    [HarmonyPatch(typeof(RangedWeapon), "SpawnProjectile")]
    public class Hook_RangedWeapon_SpawnProjectile
    {
        public static bool Prefix(Transform ___guntip)
        {
            Inventory.Shoot(___guntip);
            return false;
        }
    }
    [HarmonyPatch(typeof(RangedWeapon), "Use")]
    public class Hook_RangedWeapon_Use
    {
        public static bool Prefix()
        {
            return Inventory.CanShoot;
        }
    }
}
