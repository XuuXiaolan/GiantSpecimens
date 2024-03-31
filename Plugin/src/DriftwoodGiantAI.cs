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

namespace GiantSpecimens.Enemy {
    class DriftwoodGiantAI : EnemyAI {
        
        #pragma warning disable 0649
        public Collider AttackArea;
        public IEnumerable allAlivePlayers;
        #pragma warning restore 0649
        public string levelName;
        public bool eatingEnemy = false;
        public EnemyAI targetEnemy;
        public bool targettingEnemy;
        public PlayerControllerB targetPlayer_;
        public bool targettingPlayer;
        public float seeableDistance;
        [SerializeField] public AnimationClip spawnAnimation;
        public float spawnTime;
        [SerializeField] public AudioSource MouthVoice;
        [SerializeField] public AudioClip[] stompSounds;
        [SerializeField] public AudioClip eatenSound;
        [SerializeField] public AudioClip spawnSound;
        public bool testBuild = false; 
        public LineRenderer line;
        enum State {
            SpawnAnimation, // Spawning
            IdleAnimation, // Idling
            SearchingForPrey, // Wandering
            RunningToPrey, // Chasing
            EatingPrey, // Eating
            PlayingWithPrey, // Playing with a player's body
            Scream, // Screams and damages player
        }

        void LogIfDebugBuild(string text) {
            #if DEBUG
            Plugin.Logger.LogInfo(text);
            #endif
        }

        public override void Start()
        {
            base.Start();
            if (spawnAnimation != null) {
                spawnTime = spawnAnimation.length+1;
            }
            
            levelName = RoundManager.Instance.currentLevel.name;
            LogIfDebugBuild(levelName);

            SpawnableEnemyWithRarity DriftWoodGiant = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("DriftWoodGiant"));
            if (DriftWoodGiant != null) {
            LogIfDebugBuild(DriftWoodGiant.rarity.ToString());
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

            // walkingSpeed = Plugin.ModConfig.ConfigSpeedRedWood.Value;
            // distanceFromShip = Plugin.ModConfig.ConfigShipDistanceRedWood.Value;
            // seeableDistance = Plugin.ModConfig.ConfigForestDistanceRedWood.Value;

            LogIfDebugBuild("DriftWood Giant Enemy Spawned");
            if (testBuild) {
                #if DEBUG
                line = gameObject.AddComponent<LineRenderer>();
                line.widthMultiplier = 0.2f; // reduce width of the line
                #endif
            }

            creatureVoice.PlayOneShot(spawnSound);

            currentBehaviourStateIndex = (int)State.SpawnAnimation;
        }

        public override void Update(){
            base.Update();
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
                case (int)State.SearchingForPrey:
                    agent.speed = 5;
                    if (FindClosestTargetEnemyInRange(20f)) {

                    } else if (FindClosestPlayerInRange(20f)) {

                    }

                    break;
                case (int)State.RunningToPrey:
                    agent.speed = 20;
                    // Keep targetting target enemy, unless they are over 20 units away and we can't see them.
                    if (targettingEnemy) {
                        if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance && !HasLineOfSightToPosition(targetEnemy.transform.position) || targetEnemy == null) {
                            LogIfDebugBuild("Stop chasing target enemy");
                            DoAnimationClientRpc("startWalk");
                            StartSearch(transform.position);
                            SwitchToBehaviourClientRpc((int)State.SearchingForPrey);
                            return;
                        }
                        SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                    }
                    else if (targettingPlayer) {
                        if (Vector3.Distance(transform.position, targetPlayer_.transform.position) > seeableDistance && !HasLineOfSightToPosition(targetPlayer_.transform.position) || targetPlayer_ == null) {
                            LogIfDebugBuild("Stop chasing target player");
                            DoAnimationClientRpc("startWalk");
                            StartSearch(transform.position);
                            SwitchToBehaviourClientRpc((int)State.SearchingForPrey);
                            return;
                        }
                        SetDestinationToPosition(targetPlayer_.transform.position, checkForPath: true);
                    } else {
                        LogIfDebugBuild("If you see this, something went wrong.");
                        LogIfDebugBuild("Resettings state to Scream Animation");
                        SwitchToBehaviourClientRpc((int)State.Scream);
                    }
                    break;

                case (int)State.EatingPrey:
                    agent.speed = 0f;
                    // Does nothing so far.
					break;
                case (int)State.Scream:
                    agent.speed = 0f;
                    break;
                case (int)State.PlayingWithPrey:
                    agent.speed = 0f;
                    break;
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
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
        public void ShockwaveDamageScream() {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(15, causeOfDeath: CauseOfDeath.Blast);
                }
            }
        }

        public void ParticlesFromEatingPrey() {
           
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
        bool FindClosestTargetEnemyInRange(float range) {
            EnemyAI closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if (enemy.enemyType.enemyName == "MouthDog" || enemy.enemyType.enemyName == "BaboonHawk" || enemy.enemyType.enemyName == "Masked") { // fact check the names
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
        bool FindClosestPlayerInRange(float range) {
            PlayerControllerB closestPlayer = null;
            float minDistance = float.MaxValue;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance < range && distance < minDistance) {
                        minDistance = distance;
                        closestPlayer = player;
                    }
                }
            }
            if (closestPlayer != null) {
                targetPlayer_ = closestPlayer;
                return true;
            }
            return false;
        }
        public void PlayFootstepSound() {
            creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        }
        
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
            if (collidedEnemy == targetEnemy) {
                
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
