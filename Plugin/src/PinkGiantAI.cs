using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace GiantSpecimens {

    // You may be wondering, how does the Example Enemy know it is from class PinkGiantAI?
    // Well, we give it a reference to to this class in the Unity project where we make the asset bundle.
    // Asset bundles cannot contain scripts, so our script lives here. It is important to get the
    // reference right, or else it will not find this file. See the guide for more information.

    class PinkGiantAI : EnemyAI {

        // We set these in our Asset Bundle, so we can disable warning CS0649:
        // Field 'field' is never assigned to, and will always have its default value 'value'
        #pragma warning disable 0649
        // public Transform turnCompass
        public Transform attackArea;
        #pragma warning restore 0649
        float timer = 0;
        bool eatingEnemy = false;
        EnemyAI targetEnemy;
        [SerializeField]GameObject rightBone;
        [SerializeField]GameObject leftBone;
        [SerializeField]GameObject eatingArea;
        Vector3 midpoint;
        enum State {
            IdleAnimation, // Idling
            SearchingForForestKeeper, // Wandering
            RunningToForestKeeper, // Chasing
            EatingForestKeeper, // Eating
        }

        void LogIfDebugBuild(string text) {
            #if DEBUG
            Plugin.Logger.LogInfo(text);
            #endif
        }

        public override void Start()
        {
            base.Start();
            LogIfDebugBuild("Pink Giant Enemy Spawned");
            // creatureAnimator.SetTrigger("startWalk");

            // NOTE: Add your behavior states in your enemy script in Unity, where you can configure fun stuff
            // like a voice clip or an sfx clip to play when changing to that specific behavior state.
            currentBehaviourStateIndex = (int)State.IdleAnimation;
            // We make the enemy start searching. This will make it start wandering around.
            rightBone = GameObject.Find("Bone.005.R_end");
            leftBone = GameObject.Find("Bone.005.L_end");
            eatingArea = GameObject.Find("Bone.002");
            StartSearch(transform.position);
        }

        public override void Update(){
            base.Update();

            timer += Time.deltaTime;

            if (currentBehaviourStateIndex == (int)State.EatingForestKeeper && targetEnemy != null) {
                //gameObject.GetComponentByName("Bone.005.L_end").transform.position
                midpoint = (rightBone.transform.position + leftBone.transform.position)/2;
                targetEnemy.transform.position = midpoint + new Vector3(0,-7,0);
                targetEnemy.transform.LookAt(eatingArea.transform);
                targetEnemy.transform.rotation = Quaternion.Euler(new Vector3(0, targetEnemy.transform.rotation.eulerAngles.y + 65f, 0));
            }
        }
        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            };

            switch(currentBehaviourStateIndex) {
                case (int)State.IdleAnimation:
                    agent.speed = 0f;
                    if (timer > 3) {
                        StartCoroutine(PauseDuringIdle());
                    }
                    if (FoundForestKeeperInRange(50f)){
                        DoAnimationClientRpc("startChase");
                        LogIfDebugBuild("Start Target ForestKeeper");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.RunningToForestKeeper);
                    } // Look for Forest Keeper
                    else if (timer > 14f) {
                        DoAnimationClientRpc("startWalk");
                        LogIfDebugBuild("Start Walking Around");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForForestKeeper);
                    }
                    
                    break;
                case (int)State.SearchingForForestKeeper:
                    agent.speed = 1.5f;
                    if (FoundForestKeeperInRange(50f)){
                        DoAnimationClientRpc("startChase");
                        LogIfDebugBuild("Start Target ForestKeeper");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.RunningToForestKeeper);
                    } // Look for Forest Keeper
                    break;

                case (int)State.RunningToForestKeeper:
                    agent.speed = 6f;
                    // Keep targetting closest ForestKeeper, unless they are over 20 units away and we can't see them.
                    if (Vector3.Distance(transform.position, targetEnemy.transform.position) > 100f && !HasLineOfSightToPosition(targetEnemy.transform.position)){
                        LogIfDebugBuild("Stop Target ForestKeeper");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForForestKeeper);
                        return;
                    }
                    SetDestinationToPosition(targetEnemy.transform.position, checkForPath: false);
                    break;

                case (int)State.EatingForestKeeper:
                    agent.speed = 0f;
                    // Does nothing so far.
                    break;
                    
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
        }

        bool FoundForestKeeperInRange(float range) {
            for (int i = 0; i < RoundManager.Instance.SpawnedEnemies.Count; i++) {
                if (RoundManager.Instance.SpawnedEnemies[i].enemyType.enemyName == "ForestGiant") {
                    if (Vector3.Distance(transform.position, RoundManager.Instance.SpawnedEnemies[i].transform.position) < range) {
                        targetEnemy = RoundManager.Instance.SpawnedEnemies[i];
                        return true;
                    }
                }
            }
            return false;
        }

        IEnumerator PauseDuringIdle() {
            yield return new WaitForSeconds(3);
            StopCoroutine(PauseDuringIdle());
        }

        IEnumerator EatForestKeeper() {
            DoAnimationClientRpc("eatForestKeeper");
            targetEnemy.CancelSpecialAnimationWithPlayer();
            targetEnemy.enabled = false;
            
            LogIfDebugBuild($"{targetEnemy}");
            yield return new WaitForSeconds(10);
            targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
            yield return new WaitForSeconds(5);
            StopCoroutine(EatForestKeeper());
            eatingEnemy = false;
            SwitchToBehaviourClientRpc((int)State.IdleAnimation);
        }
        
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) 
        {
            if (collidedEnemy == targetEnemy && eatingEnemy == false) {
                LogIfDebugBuild("Pink Giant hitting this guy:" + targetEnemy);
                SwitchToBehaviourClientRpc((int)State.EatingForestKeeper);
                eatingEnemy = true;
                if (eatingEnemy) {
                    StartCoroutine(EatForestKeeper());
                }
            }
        }

        public override void OnCollideWithPlayer(Collider other) {
        PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                LogIfDebugBuild("Pink Giant Feet Collision with Player!");
                playerControllerB.DamagePlayer(100);
            }
        }
        /* private void OnTriggerStay(Collider other) {
            LogIfDebugBuild("This is happening." + other);
        } */
        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            LogIfDebugBuild($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }
    }
}