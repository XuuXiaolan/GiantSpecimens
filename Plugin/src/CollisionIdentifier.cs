using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
namespace GiantSpecimens.Collisions {
    public class ColliderIdentifier : MonoBehaviour 
    {
        [SerializeField] public AudioSource CreatureSFX;
        [SerializeField] public AudioClip squishSound;
        [SerializeField] public ParticleSystem BloodSplatterLeft;
        [SerializeField] public ParticleSystem BloodSplatterRight;
        private static readonly CauseOfDeath Thwomped = EnumUtils.Create<CauseOfDeath>("Thwomped");

        void LogIfDebugBuild(string text) {
            #if DEBUG
            Plugin.Logger.LogInfo(text);
            #endif
        } 
        private void OnTriggerEnter(Collider other)
        {
            // Check if the collider is a player or another entity you're interested in
            if (other.CompareTag("Player"))
            {
                PlayerControllerB playerControllerB = other.GetComponent<PlayerControllerB>();
                if (playerControllerB != null) {
                    // Determine which part of your GameObject caused the trigger
                    DetectCollider(this.gameObject, playerControllerB);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if the collider is a player or another entity you're interested in
            if (collision.collider.CompareTag("Player"))
            {
                PlayerControllerB playerControllerB = collision.collider.GetComponent<PlayerControllerB>();
                if (playerControllerB != null) {
                    // Determine which part of your GameObject caused the collision
                    DetectCollider(collision.gameObject, playerControllerB);
                }
            }
        }

        void DetectCollider(GameObject collidedObject, PlayerControllerB playerControllerB)
        {
            // Example: Detect which part of your GameObject caused the collision/trigger
            if (collidedObject.name == "AttackArea")
            {
                LogIfDebugBuild("Collided with AttackArea");
                // Handle AttackArea collision logic here
            }
            else if ((collidedObject.name == "CollisionFootL" || collidedObject.name == "CollisionFootR") && !playerControllerB.isInHangarShipRoom) {
                
                playerControllerB.DamagePlayer(200, causeOfDeath: Thwomped);
                CreatureSFX.PlayOneShot(squishSound);
                if (collidedObject.name == "CollisionFootL") {
                    BloodSplatterLeft.Play();
                } else {
                    BloodSplatterRight.Play();
                }
            }
            else {
                LogIfDebugBuild("Collided with unknown object: " + collidedObject.name);
            }
        }
    }
}