using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.PlayerLoop;
using System.Threading;
using Mono.Cecil.Cil;
using UnityEngine.UIElements.Experimental;

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
        public Collider AttackArea;
        public IEnumerable allAlivePlayers;
        [SerializeField] Collider CollisionFootR;
        [SerializeField] Collider CollisionFootL;
        #pragma warning restore 0649
        bool sizeUp = false;
        Vector3 newScale;
        bool eatingEnemy = false;
        EnemyAI targetEnemy;
        bool idleGiant = true;
        [SerializeField]AudioClip[] stompSounds;
        [SerializeField]AudioClip eatenSound;
        [SerializeField]AudioClip spawnSound;
        [SerializeField]GameObject rightBone;
        [SerializeField]GameObject leftBone;
        [SerializeField] GameObject eatingArea;
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
            var giantEnemyType = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("ForestGiant"));
            giantEnemyType.rarity *= Plugin.config.configSpawnrateForest.Value;
            var RedWoodGiant = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("RedWoodGiant"));
            LogIfDebugBuild(RedWoodGiant.rarity.ToString());
            LogIfDebugBuild(giantEnemyType.rarity.ToString());
            LogIfDebugBuild("Pink Giant Enemy Spawned");
            
            //LogIfDebugBuild(transform.rarity.ToString());
            creatureVoice.PlayOneShot(spawnSound);
            StartCoroutine(ScalingUp());
            // creatureAnimator.SetTrigger("startWalk");

            currentBehaviourStateIndex = (int)State.IdleAnimation;
            StartSearch(transform.position);
        }

        public override void Update(){
            base.Update();

            if (currentBehaviourStateIndex == (int)State.EatingForestKeeper && targetEnemy != null) {
                midpoint = (rightBone.transform.position + leftBone.transform.position) / 2;
                targetEnemy.transform.position = midpoint + new Vector3(0, -1f, 0);
                targetEnemy.transform.LookAt(eatingArea.transform.position);
                targetEnemy.transform.position = midpoint + new Vector3(0, -5.5f, 0);
                // Scale targetEnemy's transform down by 0.9995 everytime Update runs in this if statement
                if (!sizeUp) {
                    targetEnemy.transform.position = midpoint + new Vector3(0, -1f, 0);
                    targetEnemy.transform.LookAt(eatingArea.transform.position);
                    targetEnemy.transform.position = midpoint + new Vector3(0, -6f, 0);
                    newScale = targetEnemy.transform.localScale;
                    newScale.x *= 1.4f;
                    newScale.y *= 1.3f;
                    newScale.z *= 1.4f;
                    targetEnemy.transform.localScale = newScale;
                    sizeUp = true;
                }
                targetEnemy.transform.position += new Vector3(0, 0.02f, 0);
                newScale = targetEnemy.transform.localScale;
                newScale.x *= 0.9995f;
                newScale.y *= 0.9995f;
                newScale.z *= 0.9995f;
                targetEnemy.transform.localScale = newScale;
                targetEnemy.transform.position = Vector3.MoveTowards(targetEnemy.transform.position, eatingArea.transform.position, 0.1f);
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
                    if (idleGiant) {
                        StartCoroutine(PauseDuringIdle());
                    }
                    else if (FindClosestForestKeeperInRange(50f)){
                        DoAnimationClientRpc("startChase");
                        LogIfDebugBuild("Start Target ForestKeeper");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.RunningToForestKeeper);
                    } // Look for Forest Keeper
                    else {
                        DoAnimationClientRpc("startWalk");
                        LogIfDebugBuild("Start Walking Around");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForForestKeeper);
                    }
                    
                    break;
                case (int)State.SearchingForForestKeeper:
                    agent.speed = 1.5f;
                    if (FindClosestForestKeeperInRange(50f)){
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
                        DoAnimationClientRpc("startWalk");
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
        public void ShockwaveDamageL() {
            foreach (var player in StartOfRound.Instance.allPlayerScripts.Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)) {
                float distance = Vector3.Distance(CollisionFootL.transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15);
                }
            }
        }
        public void ShockwaveDamageR() {
            foreach (var player in StartOfRound.Instance.allPlayerScripts.Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)) {
                float distance = Vector3.Distance(CollisionFootR.transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15);
                }
            }
        }
        public void ShakePlayerCamera() {
            foreach (var player in StartOfRound.Instance.allPlayerScripts.Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)) {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                switch (distance) {
                    case < 10f:
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);

                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        break;
                    case < 20 and >= 10:
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        break;
                    case < 50f and >= 20:
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        break;
                }
            }
        }
        bool FindClosestForestKeeperInRange(float range) {
            EnemyAI closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.enemyName == "ForestGiant") {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < range && distance < minDistance) {
                        minDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            }
            if (closestEnemy != null) {
                targetEnemy = closestEnemy;
                return true;
            }
            return false;
        }

        IEnumerator ScalingUp() {
            newScale = transform.localScale;
            newScale.x *= 0.1f;
            newScale.y *= 0.1f;
            newScale.z *= 0.1f;
            transform.localScale = newScale;
            const float AnimationDuration = 10f; // measured in seconds
            float elapsedTime = 0;

            float startScale = 0.01f;
            float endScale = 1f;

            while (elapsedTime < AnimationDuration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(elapsedTime / AnimationDuration);
                float currentScale = Mathf.Lerp(startScale, endScale, lerpFactor);
                transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            }
            StopCoroutine(ScalingUp());
        }
        IEnumerator PauseDuringIdle() {
            yield return new WaitForSeconds(14);
            idleGiant = false;
            StopCoroutine(PauseDuringIdle());
        }
        public void PlayFootstepSound() {
            creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        }
        IEnumerator EatForestKeeper() {
            Collider[] colliders = targetEnemy.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
            targetEnemy.agent.enabled = false;
            SwitchToBehaviourClientRpc((int)State.EatingForestKeeper);
            DoAnimationClientRpc("eatForestKeeper");
            targetEnemy.CancelSpecialAnimationWithPlayer();
            targetEnemy.SetEnemyStunned(true, 10f);
            targetEnemy.creatureVoice.Stop();
            targetEnemy.creatureSFX.Stop();
            targetEnemy.creatureVoice.PlayOneShot(eatenSound);
            yield return new WaitForSeconds(11);
            targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
            yield return new WaitForSeconds(4);
            StopCoroutine(EatForestKeeper());
            eatingEnemy = false;
            idleGiant = true;
            sizeUp = false;
            SwitchToBehaviourClientRpc((int)State.IdleAnimation);
        }
        
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) 
        {
            if (collidedEnemy == targetEnemy && eatingEnemy == false) {
                eatingEnemy = true;
                if (eatingEnemy) {
                    StartCoroutine(EatForestKeeper());
                }
            }
        }
        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            // LogIfDebugBuild($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }
    }
}