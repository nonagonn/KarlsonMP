using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KarlsonMP
{
    public class AnimController : MonoBehaviour
    {
        public ushort kmpid;
        public Animator Animator = null;

        void Start()
        {
            Animator = GetComponent<Animator>();
        }

        public bool isCrouched = false;
        public bool isMoving = false;
        public bool isKnife = false;
        public bool isGrounded = false;

        void FixedUpdate()
        {
            if (Animator == null) return;
            bool inAir = !isGrounded;
            Animator.SetBool("InAir", inAir);
            Animator.SetBool("IsWalking", isMoving && !inAir);
            Animator.SetBool("IsCrouch", isCrouched);
            Animator.SetBool("HasKnife", isKnife);
        }
    }

    public class Player
    {
        public int id;
        public string username;
        public GameObject player;

        private float collOldRot = 0;

        public Player(ushort _id, string _username)
        {
            try
            {
                id = _id;
                username = _username;
                player = UnityEngine.Object.Instantiate(MonoHooks.playerPrefab);
                player.name = username + " [prefab]";
                player.AddComponent<AnimController>().kmpid = _id;
                // give gun
                Transform gunPosition = player.transform.Find("Armature/Hips.Control/Hips/Waist/Torso/LeftShoulderJoint/Shoulder.L/UpperArm.L/LowerArm.L/Hand.L/PistolPosition");
                GameObject pistol = KMP_PrefabManager.NewPistol();
                UnityEngine.Object.Destroy(pistol.GetComponent<Rigidbody>());
                pistol.GetComponent<Pickup>().PickupWeapon(false);
                pistol.transform.parent = gunPosition;
                pistol.transform.localPosition = new Vector3(1f, -.5f, 0f);
                pistol.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                pistol.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            catch (System.Exception e)
            {
                KMP_Console.Log(e.ToString());
            }
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(player);
        }

        private int stallMovement = 0;
        private bool crouch, moving, grounded;
        private float animation_rotX;
        public void UpdateAnimations(bool _crouch, bool _moving, bool _grounded)
        {
            crouch = _crouch;
            moving = _moving;
            grounded = _grounded;

            player.GetComponent<AnimController>().isCrouched = crouch;
            player.GetComponent<AnimController>().isMoving = moving;
            player.GetComponent<AnimController>().isGrounded = grounded;

            stallMovement++;
            if (stallMovement < 30) return; // hold rotation for some frames for animations
            
            // if not knife
            Transform shoulderL = player.transform.Find("Armature/Hips.Control/Hips/Waist/Torso/LeftShoulderJoint/Shoulder.L");
            Transform shoulderR = player.transform.Find("Armature/Hips.Control/Hips/Waist/Torso/RightShoulderJoint/Shoulder.R");
            Transform neck = player.transform.Find("Armature/Hips.Control/Hips/Waist/Torso/Neck");
            Vector3 point = neck.position + new Vector3(0f, -0.02f, 0f);

            shoulderL.RotateAround(point, player.transform.right, animation_rotX - collOldRot);
            shoulderR.RotateAround(point, player.transform.right, animation_rotX - collOldRot);
            neck.RotateAround(point, player.transform.right, animation_rotX - collOldRot);
            collOldRot = animation_rotX;
        }

        public void Move(Vector3 basicPos, Vector2 rot)
        {
            if (!crouch)
                basicPos += new Vector3(0, 1.2f, 0f);
            else
                basicPos += new Vector3(0, 0.9f, 0f);
            player.transform.position = basicPos;
            player.transform.rotation = Quaternion.Euler(0, rot.y, 0);
            animation_rotX = rot.x;
        }
    }
}
