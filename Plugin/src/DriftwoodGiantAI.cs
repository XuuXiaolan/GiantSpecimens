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
        [NonSerialized]
        public IEnumerable allAlivePlayers;
        #pragma warning restore 0649
        [NonSerialized]
        public string levelName;
        [NonSerialized]
        public bool eatingEnemy = false;
        [NonSerialized]
        public EnemyAI targetEnemy;
        [NonSerialized]
        public bool targettingEnemy;
        [NonSerialized]
        public PlayerControllerB targetPlayer_;
        [NonSerialized]
        public bool targettingPlayer;
        [NonSerialized]
        public float seeableDistance;
        public AnimationClip spawnAnimation;
        [NonSerialized]
        public bool spawned = false;
        [NonSerialized]
        public bool screaming = false;
        public AudioSource MouthVoice;
        public AudioClip[] stompSounds;
        public AudioClip eatenSound;
        public AudioClip screamSound;
        public AudioClip spawnSound;
        [NonSerialized]
        public bool holdPlayer = false;
        [NonSerialized]
        public int previousStateIndex = 0;
        [NonSerialized]
        public int nextStateIndex = 0;
        [NonSerialized]
        public string nextAnimationName = "";
        [NonSerialized]
        public bool testBuild = false; 
        [NonSerialized]
        private System.Random throwRandom;
        [NonSerialized]
        public LineRenderer line;
        enum State {
            SpawnAnimation, // Spawning
            IdleAnimation, // Idling
            SearchingForPrey, // Wandering
            RunningToPrey, // Chasing
            SlashingPrey, // Hitting prey
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
            throwRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
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
            StartCoroutine(SpawnAnimationCooldown());
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
                        // chase the target enemy.
                        // SCREAM
                        StopSearch(currentSearch);
                        previousBehaviourStateIndex = currentBehaviourStateIndex;
                        nextStateIndex = (int)State.RunningToPrey;
                        nextAnimationName = "startChase";
                        DoAnimationClientRpc("startScream");
                        SwitchToBehaviourClientRpc((int)State.Scream);
                        
                    } else if (FindClosestPlayerInRange(20f)) {
                        // chase the target player.
                        // SCREAM
                        StopSearch(currentSearch);
                        previousBehaviourStateIndex = currentBehaviourStateIndex;
                        nextStateIndex = (int)State.RunningToPrey;
                        nextAnimationName = "startChase";
                        DoAnimationClientRpc("startScream");
                        SwitchToBehaviourClientRpc((int)State.Scream);
                    }

                    break;
                case (int)State.RunningToPrey:
                    agent.speed = 20;
                    // Keep targetting target enemy, unless they are over 20 units away and we can't see them.
                    if (targettingEnemy) {
                        if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance && !HasLineOfSightToPosition(targetEnemy.transform.position) || targetEnemy == null) {
                            LogIfDebugBuild("Stop chasing target enemy");
                            StartSearch(transform.position);
                            targettingEnemy = false;
                            
                            previousBehaviourStateIndex = currentBehaviourStateIndex;
                            nextStateIndex = (int)State.SearchingForPrey;
                            nextAnimationName = "startWalk";
                            DoAnimationClientRpc("startScream");
                            SwitchToBehaviourClientRpc((int)State.Scream);
                            return;
                        }
                        SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                    }
                    else if (targettingPlayer) {
                        if (Vector3.Distance(transform.position, targetPlayer_.transform.position) > seeableDistance && !HasLineOfSightToPosition(targetPlayer_.transform.position) || targetPlayer_ == null) {
                            LogIfDebugBuild("Stop chasing target player");
                            DoAnimationClientRpc("startWalk");
                            StartSearch(transform.position);
                            targettingPlayer = false;

                            previousBehaviourStateIndex = currentBehaviourStateIndex;
                            nextStateIndex = (int)State.SearchingForPrey;
                            nextAnimationName = "startWalk";
                            DoAnimationClientRpc("startScream");
                            SwitchToBehaviourClientRpc((int)State.Scream);
                            return;
                        }
                        SetDestinationToPosition(targetPlayer_.transform.position, checkForPath: true);
                    } else {
                        LogIfDebugBuild("If you see this, something went wrong.");
                        LogIfDebugBuild("Resettings state to Scream Animation");
                        previousBehaviourStateIndex = currentBehaviourStateIndex;
                        SwitchToBehaviourClientRpc((int)State.Scream);
                    }
                    break;
                case (int)State.SlashingPrey:
                    agent.speed = 0f;
                    break;
                case (int)State.EatingPrey:
                    agent.speed = 0f;
					break;
                case (int)State.Scream:
                    agent.speed = 0f;
                    // Does nothing, on purpose.
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

        public void ParticlesFromEatingPrey() {
           // Make some like, red, steaming hot particles come out of the enemy corpses.
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
                if (enemy.enemyType.enemyName == "MouthDog" || enemy.enemyType.enemyName == "Baboon hawk" || enemy.enemyType.enemyName == "Masked") { // fact check the names
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < range && distance < minDistance) {
                        minDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            }
            if (closestEnemy != null) {
                targetEnemy = closestEnemy;
                targettingEnemy = true;
                return true;
            }
            return false;
        }
        bool FindClosestPlayerInRange(float range) {
            PlayerControllerB closestPlayer = null;
            float minDistance = float.MaxValue;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom) {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance < range && distance < minDistance) {
                        minDistance = distance;
                        closestPlayer = player;
                    }
                }
            }
            if (closestPlayer != null) {
                targetPlayer_ = closestPlayer;
                targettingPlayer = true;
                return true;
            }
            return false;
        }
        public void DriftwoodScream() {
            ScreamPause();
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= 10f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(25, causeOfDeath: Plugin.RupturedEardrums);
                }
            }
        }
        public void PlayFootstepSound() {
            creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        }
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
            if (collidedEnemy == targetEnemy && targetEnemy != null && currentBehaviourStateIndex == (int)State.RunningToPrey) {
                SwitchToBehaviourClientRpc((int)State.SlashingPrey);
                DoAnimationClientRpc("startSlash");
            }
        }
        public void SlashEnemy() {
            // Do Chain IK stuff later, see PinkGiantAI.cs for reference.
            if (targettingEnemy) {
                targetEnemy.HitEnemy(1);
                if (targetEnemy.enemyHP <= 0) {
                    nextStateIndex = (int)State.EatingPrey;
                    nextAnimationName = "startEating";
                    targettingEnemy = false;
                    targetEnemy = null;
                }
            } else {
                LogIfDebugBuild("This shouldn't be happening, please report this.");
            }
        }
        public override void OnCollideWithPlayer(Collider other) {
            if (other.GetComponent<PlayerControllerB>() == targetPlayer_) {
                StartCoroutine(ThrowPlayer());
            }
        }
        public void ThrowingPlayer() {
            if (targetPlayer_ == null) return; // No player to throw

            // Calculate the throwing direction
            float randomAngle = throwRandom.Next(-4, 5) * (float)Math.PI / 8; // Random angle between -40 degrees and 40 degrees
            Vector3 throwingDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.up;
            // Throw the player
            targetPlayer_.GetComponent<Rigidbody>().velocity = throwingDirection * 15; // Apply velocity to the player in the calculated direction

            // Reset targeting
            targettingPlayer = false;
            targetPlayer_ = null;
        }
        IEnumerator ThrowPlayer() {
            // Make it so that it waits until the animation is over before throwing the player.
            holdPlayer = true;
            yield return new WaitForSeconds(4f);
            holdPlayer = false;
            ThrowingPlayer();
            StopCoroutine(ThrowPlayer());
        }
        IEnumerator ScreamPause() {
            // yield return new WaitForSeconds(screamSound.length);
            yield return new WaitForSeconds(3);
            DoAnimationClientRpc(nextAnimationName);
            SwitchToBehaviourClientRpc(nextStateIndex);
            StopCoroutine(ScreamPause());
        }
        IEnumerator SpawnAnimationCooldown() {
            // yield return new WaitForSeconds(spawnAnimation.length);
            yield return new WaitForSeconds(3);
            previousBehaviourStateIndex = currentBehaviourStateIndex;
            StartSearch(transform.position);
            DoAnimationClientRpc("startWalk");
            SwitchToBehaviourClientRpc((int)State.SearchingForPrey);
            StopCoroutine(SpawnAnimationCooldown());
        }
        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            LogIfDebugBuild($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }
    }
}
