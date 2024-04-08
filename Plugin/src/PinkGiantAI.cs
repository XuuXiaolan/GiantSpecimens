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
using System.Text.RegularExpressions;
using UnityEngine.AI;
using GiantSpecimens.Colours;
using System.Reflection;

namespace GiantSpecimens.Enemy {
    class PinkGiantAI : EnemyAI {
        
        // We set these in our Asset Bundle, so we can disable warning CS0649:
        // Field 'field' is never assigned to, and will always have its default value 'value'
        #pragma warning disable 0649
        [NonSerialized]
        public static LevelColorMapper levelColorMapper = new();
        public Collider AttackArea;
        public ParticleSystem DustParticlesLeft;
        public ParticleSystem DustParticlesRight;
        public ParticleSystem ForestKeeperParticles;
        public Collider CollisionFootR;
        public Collider CollisionFootL;
        public ChainIKConstraint LeftFoot;
        public ChainIKConstraint RightFoot;
        public AudioSource FootSource;
        public AudioSource EnemyMouthSource;
        #pragma warning restore 0649
        [NonSerialized]
        public bool sizeUp = false;
        [NonSerialized]
        public Vector3 newScale;
        [NonSerialized]
        public string levelName;
        [NonSerialized]
        public bool waitAfterChase = false;
        [NonSerialized]
        public bool eatingEnemy = false;
        [NonSerialized]
        public string footstepColour;
        [NonSerialized]
        public EnemyAI targetEnemy;
        [NonSerialized]
        public bool idleGiant = true;
        [NonSerialized]
        public float walkingSpeed;
        [NonSerialized]
        public float seeableDistance;
        [NonSerialized]
        public float distanceFromShip;
        [NonSerialized]
        public float distanceFromEnemy;
        [NonSerialized]
        public Transform shipBoundaries;
        public AnimationClip idle;
        public AnimationClip walking;
        public AnimationClip eating;
        public AnimationClip roaring;
        public AudioClip[] stompSounds;
        public AudioClip eatenSound;
        public AudioClip spawnSound;
        public AudioClip roarSound;
        public GameObject rightBone;
        public GameObject leftBone;
        public GameObject eatingArea;
        [NonSerialized]
        public Vector3 midpoint;
        [NonSerialized]
        public bool testBuild = false; 
        [NonSerialized]
        public LineRenderer line;
        enum State {
            SpawnAnimation, // Roaring
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
            shipBoundaries = StartOfRound.Instance.shipBounds.transform;
            shipBoundaries.localScale *= 1.5f;
            
            
            Color dustColor = Color.grey; // Default to grey if no color found
            string footstepColourValue = Plugin.ModConfig.ConfigColourHexcode.Value;
            if (string.IsNullOrEmpty(footstepColourValue)) {
                footstepColour = null;
            } else if (Regex.IsMatch(footstepColourValue, "^#?[0-9a-fA-F]{6}$")) {
                footstepColour = footstepColourValue;
            } else {
                Plugin.Logger.LogWarning("Invalid hexcode: " + footstepColourValue + ". Using default colour.");
                footstepColour = null;
            }
            List<string> colorsForCurrentLevel = levelColorMapper.GetColorsForLevel(levelName);
            if (footstepColour == null && colorsForCurrentLevel.Count > 0) {
                footstepColour = colorsForCurrentLevel[0];
            }
            if (footstepColour != null) {
                dustColor = HexToColor(footstepColour);
            }
            MainModule mainLeft = DustParticlesLeft.main;
            MainModule mainRight = DustParticlesRight.main;
            mainLeft.startColor = new MinMaxGradient(dustColor);
            mainRight.startColor = new MinMaxGradient(dustColor);
            LogIfDebugBuild(dustColor.ToString());

            SpawnableEnemyWithRarity giantEnemyType = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("ForestGiant"));
            if (giantEnemyType != null && Plugin.ModConfig.ConfigMultiplierForestkeeper.Value >= 0) {
                giantEnemyType.rarity *= Plugin.ModConfig.ConfigMultiplierForestkeeper.Value;                
            }
            SpawnableEnemyWithRarity DriftWoodGiant = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("DriftWoodGiant"));
            if (DriftWoodGiant != null && Plugin.ModConfig.ConfigMultiplierDriftwood.Value >= 0) {
                DriftWoodGiant.rarity *= Plugin.ModConfig.ConfigMultiplierDriftwood.Value;
            }
            SpawnableEnemyWithRarity RedWoodGiant = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("RedWoodGiant"));
            if (RedWoodGiant != null) {
            LogIfDebugBuild(RedWoodGiant.rarity.ToString());
            }

            /* foreach(SpawnableEnemyWithRarity enemy in RoundManager.Instance.currentLevel.OutsideEnemies) {
                if(enemy != null) {
                    LogIfDebugBuild("Enemy: " + enemy.enemyType.enemyName);
                }
            }
            foreach(SpawnableEnemyWithRarity enemy in RoundManager.Instance.currentLevel.Enemies) {
                if(enemy != null) {
                    LogIfDebugBuild("Enemy: " + enemy.enemyType.enemyName);
                }
            } */
            walkingSpeed = Plugin.ModConfig.ConfigSpeedRedWood.Value;
            distanceFromShip = Plugin.ModConfig.ConfigShipDistanceRedWood.Value;
            seeableDistance = Plugin.ModConfig.ConfigForestDistanceRedWood.Value;

            // LogIfDebugBuild(giantEnemyType.rarity.ToString());
            LogIfDebugBuild("Pink Giant Enemy Spawned");
            if (testBuild) {
                #if DEBUG
                line = gameObject.AddComponent<LineRenderer>();
                line.widthMultiplier = 0.2f; // reduce width of the line
                #endif
            }

            FootSource.PlayOneShot(spawnSound);
            EnemyMouthSource.PlayOneShot(roarSound);
            StartCoroutine(ScalingUp());
            SwitchToBehaviourClientRpc((int)State.SpawnAnimation);
        }

        public override void Update() {
            base.Update();

            if (currentBehaviourStateIndex == (int)State.EatingForestKeeper && targetEnemy != null) {
                midpoint = leftBone.transform.position;
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
            } //Should I have this whole thing be in AIInterval instead?
        }
        public void SearchOrChaseTarget() {
            DoAnimationClientRpc("startWalk");
            LogIfDebugBuild("Start Walking Around");
            StartSearch(transform.position);
            SwitchToBehaviourClientRpc((int)State.SearchingForForestKeeper);
        }
        public override void DoAIInterval()
        {
            
            base.DoAIInterval();
            if (testBuild) { 
                StartCoroutine(DrawPath(line, agent));
            }
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            };
            switch(currentBehaviourStateIndex) {
                case (int)State.SpawnAnimation:
                    agent.speed = 0f;
                    break;
                case (int)State.IdleAnimation:
                    agent.speed = 0f;
                    break;
                case (int)State.SearchingForForestKeeper:
                    agent.speed = walkingSpeed;
                    if (FindClosestAliveForestKeeperInRange(seeableDistance)){
                        DoAnimationClientRpc("startChase");
                        StartCoroutine(ChaseCoolDown());
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
                        .FirstOrDefault(x => RWHasLineOfSightToPosition(x.Player.transform.position));
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
                    if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance && !RWHasLineOfSightToPosition(targetEnemy.transform.position) || targetEnemy == null || Vector3.Distance(targetEnemy.transform.position, shipBoundaries.position) <= distanceFromShip || targetEnemy.enemyHP <= 0) {
                        LogIfDebugBuild("Stop Target ForestKeeper");
                        DoAnimationClientRpc("startWalk");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForForestKeeper);
                        return;
                    }
                    SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
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
        public static IEnumerator DrawPath(LineRenderer line, NavMeshAgent agent) {
			if (!agent.enabled) yield break;
			yield return new WaitForEndOfFrame();
			line.SetPosition(0, agent.transform.position); //set the line's origin

			line.positionCount = agent.path.corners.Length; //set the array of positions to the amount of corners
			for (var i = 1; i < agent.path.corners.Length; i++)
			{
				line.SetPosition(i, agent.path.corners[i]); //go through each corner and set that to the line renderer's position
			}
		}
        public void DealEnemyDamageFromShockwave(EnemyAI enemy, string foot) {
            if (foot == "LeftFoot") {
                distanceFromEnemy = Vector3.Distance(CollisionFootL.transform.position, enemy.transform.position);
            } else if (foot == "RightFoot") {
                distanceFromEnemy = Vector3.Distance(CollisionFootR.transform.position, enemy.transform.position);
            }

            if (distanceFromEnemy <= 3f) {
                enemy.HitEnemy(2, null, false);
            } else if (distanceFromEnemy <= 10f) {
                enemy.HitEnemy(1, null, false);
            }
            // LogIfDebugBuild($"Distance: {distanceFromEnemy} HP: {enemy.enemyHP}");
        }

        public void LeftFootStepInteractions() {
            DustParticlesLeft.Play(); // Play the particle system with the updated color
            FootSource.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
                float distance = Vector3.Distance(CollisionFootL.transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15, causeOfDeath: Plugin.InternalBleed);
                }
            }
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.canDie && enemy.enemyHP > 1 && !enemy.isEnemyDead && enemy.enemyType.enemyName != "ForestGiant") {
                    DealEnemyDamageFromShockwave(enemy, "LeftFoot");
                }
            }
        }

        public void RightFootStepInteractions() {
            DustParticlesRight.Play(); // Play the particle system with the updated color
            FootSource.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
                float distance = Vector3.Distance(CollisionFootR.transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15, causeOfDeath: Plugin.InternalBleed);
                }
            }
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.canDie && enemy.enemyHP > 1 && !enemy.isEnemyDead && enemy.enemyType.enemyName != "ForestGiant") {
                    DealEnemyDamageFromShockwave(enemy, "RightFoot");
                }
            }
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
            ForestKeeperParticles.Play(); // Also make them be affected by the world for proper fog stuff?
        }
        
        public void ShakePlayerCamera() {
                float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
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
        bool FindClosestAliveForestKeeperInRange(float range) {
            EnemyAI closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.enemyName == "ForestGiant" && enemy.enemyHP > 0 && !enemy.isEnemyDead) {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < range && distance < minDistance && Vector3.Distance(enemy.transform.position, shipBoundaries.position) > distanceFromShip) {
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
        IEnumerator ChaseCoolDown() {
            yield return new WaitForSeconds(3.5f);
            waitAfterChase = true;
            StopCoroutine(ChaseCoolDown());
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
            yield return new WaitForSeconds(idle.length);
            StopCoroutine(PauseDuringIdle());
        }
        IEnumerator EatForestKeeper() {
            targetEnemy.currentBehaviourStateIndex = 0;
            targetEnemy.CancelSpecialAnimationWithPlayer();
            targetEnemy.StopAllCoroutines();
            targetEnemy.SetEnemyStunned(true, 10f);
            targetEnemy.creatureVoice.Stop();
            targetEnemy.creatureSFX.Stop();
            EnemyMouthSource.PlayOneShot(eatenSound, 1);
            SwitchToBehaviourClientRpc((int)State.EatingForestKeeper);
            DoAnimationClientRpc("eatForestKeeper");
            yield return new WaitForSeconds(eating.length);
            StartCoroutine(PauseDuringIdle());
            SwitchToBehaviourClientRpc((int)State.IdleAnimation);
            StopCoroutine(EatForestKeeper());
        }
        public void EatingTargetGiant() {
            targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
            eatingEnemy = false;
            sizeUp = false;
            waitAfterChase = false;
        }
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)  {
            if (collidedEnemy == targetEnemy && !eatingEnemy && (currentBehaviourStateIndex == (int)State.RunningToForestKeeper) && waitAfterChase) {
                eatingEnemy = true;
                targetEnemy.GetComponent<BoxCollider>().enabled = false;
                targetEnemy.transform.Find("FGiantModelContainer").Find("AnimContainer").Find("metarig").Find("spine").Find("spine.003").Find("shoulder.R").Find("upper_arm.R").Find("forearm.R").Find("hand.R").GetComponent<BoxCollider>().enabled = false;
                targetEnemy.transform.Find("FGiantModelContainer").GetComponent<CapsuleCollider>().enabled = false;
                targetEnemy.agent.enabled = false;
                StartCoroutine(EatForestKeeper());
            }
        }

        public bool RWHasLineOfSightToPosition(Vector3 pos, float width = 45f, int range = 60, float proximityAwareness = -1f) {
            if (eye == null) {
                _ = transform;
            } else {
                _ = eye;
            }

            if (Vector3.Distance(eye.position, pos) < (float)range && !Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
                Vector3 to = pos - eye.position;
                if (Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness) {
                    return true;
                }
            }
            return false;
        }
        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            LogIfDebugBuild($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }
    }
}
