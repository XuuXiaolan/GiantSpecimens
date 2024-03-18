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
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using UnityEngine.Animations.Rigging;

namespace GiantSpecimens {
    class PinkGiantAI : EnemyAI {
        
        // We set these in our Asset Bundle, so we can disable warning CS0649:
        // Field 'field' is never assigned to, and will always have its default value 'value'
        #pragma warning disable 0649
        public static LevelColorMapper levelColorMapper = new LevelColorMapper();
        public Collider AttackArea;
        public IEnumerable allAlivePlayers;
        [SerializeField] ParticleSystem DustParticlesLeft;
        [SerializeField] ParticleSystem DustParticlesRight;
        [SerializeField] ParticleSystem ForestKeeperParticles;
        [SerializeField] Collider CollisionFootR;
        [SerializeField] Collider CollisionFootL;
        [SerializeField] ChainIKConstraint LeftFoot;
        [SerializeField] ChainIKConstraint RightFoot;
        #pragma warning restore 0649
        bool sizeUp = false;
        Vector3 newScale;
        string levelName;
        bool eatingEnemy = false;
        EnemyAI targetEnemy;
        bool idleGiant = true;
        bool waitAfterChase = false;
        float walkingSpeed;
        float seeableDistance;
        public float distanceFromShip;
        public Vector3 ship;
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
            
            levelName = RoundManager.Instance.currentLevel.name;
            LogIfDebugBuild(levelName);
            ship = StartOfRound.Instance.elevatorTransform.position;

            List<string> colorsForCurrentLevel = levelColorMapper.GetColorsForLevel(levelName);
            Color dustColor = Color.white; // Default to white if no color found
            if (colorsForCurrentLevel.Count > 0) {
                dustColor = HexToColor(colorsForCurrentLevel[0]);
            }
            MainModule mainLeft = DustParticlesLeft.main;
            MainModule mainRight = DustParticlesRight.main;
            mainLeft.startColor = new MinMaxGradient(dustColor);
            mainRight.startColor = new MinMaxGradient(dustColor);
            LogIfDebugBuild(dustColor.ToString());

            SpawnableEnemyWithRarity giantEnemyType = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("ForestGiant"));
            if (giantEnemyType != null) {
                giantEnemyType.rarity *= Plugin.config.configSpawnrateForest.Value;                
            }
            SpawnableEnemyWithRarity RedWoodGiant = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("RedWoodGiant"));
            if (RedWoodGiant != null) {
            LogIfDebugBuild(RedWoodGiant.rarity.ToString());
            }
            walkingSpeed = Plugin.config.configSpeedRedWood.Value;
            distanceFromShip = Plugin.config.configShipDistanceRedWood.Value;
            seeableDistance = Plugin.config.configForestDistanceRedWood.Value;

            // LogIfDebugBuild(giantEnemyType.rarity.ToString());
            LogIfDebugBuild("Pink Giant Enemy Spawned");

            creatureVoice.PlayOneShot(spawnSound);
            transform.position += new Vector3(0f, 10f, 0f);
            StartCoroutine(ScalingUp());
            StartCoroutine(PauseDuringIdle());

            currentBehaviourStateIndex = (int)State.IdleAnimation;
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
                // targetEnemy.transform.position = Vector3.MoveTowards(targetEnemy.transform.position, eatingArea.transform.position, 0.5f);
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
                    if (FindClosestForestKeeperInRange(seeableDistance) && !idleGiant){
                        DoAnimationClientRpc("startChase");
                        StartCoroutine(chaseCoolDown());
                        LogIfDebugBuild("Start Target ForestKeeper");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.RunningToForestKeeper);
                    } // Look for Forest Keeper
                    else if (!idleGiant) {
                        DoAnimationClientRpc("startWalk");
                        LogIfDebugBuild("Start Walking Around");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForForestKeeper);
                    }
                    break;
                case (int)State.SearchingForForestKeeper:
                    agent.speed = walkingSpeed;
                    if (FindClosestForestKeeperInRange(seeableDistance) && !idleGiant){
                        DoAnimationClientRpc("startChase");
                        StartCoroutine(chaseCoolDown());
                        LogIfDebugBuild("Start Target ForestKeeper");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.RunningToForestKeeper);
                    } // Look for Forest Keeper
                    var closestPlayer = StartOfRound.Instance.allPlayerScripts
                        .Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)
                        .Select(player => new {
                            Player = player,
                            Distance = Vector3.Distance(player.transform.position, transform.position)
                        })
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault(x => HasLineOfSightToPosition(x.Player.transform.position));
                    if (closestPlayer != null) {
                        ChainIKConstraint closestFoot = GetClosestCollisionFoot(transform.position);
                        if (closestFoot != null && closestFoot.data.target == null) {
                            closestFoot.data.target = closestPlayer.Player.transform;
                        }
                    }
                    else {
                        ChainIKConstraint closestFoot = GetClosestCollisionFoot(transform.position);
                        if (closestFoot != null) {
                            closestFoot.data.target = null;
                        }
                    }
                    break;
                case (int)State.RunningToForestKeeper:
                    agent.speed = walkingSpeed * 4;
                    // Keep targetting closest ForestKeeper, unless they are over 20 units away and we can't see them.
                    if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance && !HasLineOfSightToPosition(targetEnemy.transform.position) || targetEnemy == null || Vector3.Distance(targetEnemy.transform.position, ship) <= distanceFromShip) {
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
        public ChainIKConstraint GetClosestCollisionFoot(Vector3 position) {
            if (Vector3.Distance(position, transform.position) <= 30) {
                if (Vector3.Distance(CollisionFootL.transform.position, position) < Vector3.Distance(CollisionFootR.transform.position, position)) {
                    return LeftFoot;
                }
                return RightFoot;
            }
            return null;
        }
        public void ShockwaveDamageL() {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts.Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)) {
                float distance = Vector3.Distance(CollisionFootL.transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15);
                }
            }
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.enemyName == "MouthDog" && enemy.enemyHP > 4) {
                    float distance = Vector3.Distance(CollisionFootL.transform.position, enemy.transform.position);
                    if (distance <= 3f) {
                        enemy.HitEnemy(2);
                    } else if (distance <= 10f) {
                        enemy.HitEnemy(1);
                    }
                    LogIfDebugBuild($"Distance: {distance} HP: {enemy.enemyHP}");
                }
            }
        }
        public void ShockwaveDamageR() {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts.Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)) {
                float distance = Vector3.Distance(CollisionFootR.transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15);
                }
            }
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.enemyName == "MouthDog" && enemy.enemyHP > 4) {
                    float distance = Vector3.Distance(CollisionFootR.transform.position, enemy.transform.position);
                    if (distance <= 3f) {
                        enemy.HitEnemy(2);
                    } else if (distance <= 10f) {
                        enemy.HitEnemy(1);
                    }
                    LogIfDebugBuild($"Distance: {distance} HP: {enemy.enemyHP}");
                }
            }
        }

        public void DustFromLeftFootstep() {
            DustParticlesLeft.Play(); // Play the particle system with the updated color
        }

        public void DustFromRightFootstep() {
            DustParticlesRight.Play(); // Play the particle system with the updated color
        }

        private Color HexToColor(string hexCode) {
            Color color;

            if (ColorUtility.TryParseHtmlString(hexCode, out color))
            {
                return color;
            }
            else
            {
                return Color.white; // Default color if parsing fails
            }
        }
        public void ParticlesFromEatingForestKeeper() {
            ForestKeeperParticles.Play(); // Make the player unable to interact with these particles, same with footstep ones, also make them be affected by the world for proper fog stuff?
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

            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.enemyName == "ForestGiant") {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < range && distance < minDistance && Vector3.Distance(enemy.transform.position, ship) > distanceFromShip) {
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
        IEnumerator chaseCoolDown() {
            yield return new WaitForSeconds(2);
            waitAfterChase = true;
            StopCoroutine(chaseCoolDown());
        }
        IEnumerator PauseDuringIdle() {
            yield return new WaitForSeconds(15);
            idleGiant = false;
            StopCoroutine(PauseDuringIdle());
        }
        public void PlayFootstepSound() {
            creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        }
        IEnumerator EatForestKeeper() {
            targetEnemy.agent.enabled = false;
            SwitchToBehaviourClientRpc((int)State.EatingForestKeeper);            
            DoAnimationClientRpc("eatForestKeeper");
            targetEnemy.CancelSpecialAnimationWithPlayer();
            targetEnemy.SetEnemyStunned(true, 10f);
            targetEnemy.creatureVoice.Stop();
            targetEnemy.creatureSFX.Stop();
            targetEnemy.creatureVoice.PlayOneShot(eatenSound);
            yield return new WaitForSeconds(10);
            targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
            eatingEnemy = false;
            sizeUp = false;
            idleGiant = true;
            waitAfterChase = false;
            yield return new WaitForSeconds(2);
            StartCoroutine(PauseDuringIdle());
            SwitchToBehaviourClientRpc((int)State.IdleAnimation);
            StopCoroutine(EatForestKeeper());
        }
        
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) 
        {
            if (collidedEnemy == targetEnemy && !eatingEnemy && !(currentBehaviourStateIndex == (int)State.IdleAnimation) && waitAfterChase == true) {
                eatingEnemy = true;
                Collider[] colliders = targetEnemy.GetComponents<Collider>();
                foreach (Collider collider in colliders)
                {
                    collider.enabled = false;
                }
                StartCoroutine(EatForestKeeper());
            }
        }
        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            LogIfDebugBuild($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }
    }
}