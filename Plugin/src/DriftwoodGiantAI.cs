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
using System.Reflection;
using GiantSpecimens.Patches;

namespace GiantSpecimens.Enemy {
    class DriftwoodGiantAI : EnemyAI {
        #pragma warning disable 0649
        public Collider grabArea;
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
        public bool targettingEnemy = false;
        [NonSerialized]
        public PlayerControllerB targetPlayer_;
        [NonSerialized]
        public bool targettingPlayer = false;
        [NonSerialized]
        public float seeableDistance = 50f;
        public AnimationClip spawnAnimation;
        public AnimationClip screamAnimation;
        public AnimationClip throwAnimation;
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
        public Vector3 playerPositionBeforeGrab;
        [NonSerialized]
        public Vector3 enemyPositionBeforeDeath;
        [NonSerialized]
        public bool currentlyGrabbed = false;
        [NonSerialized]
        public int previousStateIndex = 0;
        [NonSerialized]
        public int nextStateIndex = 0;
        [NonSerialized]
        public string nextAnimationName = "";
        [NonSerialized]
        public bool testBuild = true; 
        [NonSerialized]
        private System.Random throwRandom;
        [NonSerialized]
        public float delayedResponse;
        [NonSerialized]
        public bool tooFarAway = false;
        [NonSerialized]
        public int numberOfFeedings = 0;
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
            SwitchToBehaviourClientRpc((int)State.SpawnAnimation);
        }
        public override void Update() {
            base.Update();
            if (GameNetworkManager.Instance.localPlayerController != null && DWHasLineOfSightToPosition(GameNetworkManager.Instance.localPlayerController.transform.position)) {
                DriftwoodGiantSeePlayerEffect();
            }
            if (IsOwner && enemyHP <= 0 && !isEnemyDead) {
                isEnemyDead = true;
                KillEnemyOnOwnerClient(false);
                DoAnimationClientRpc("startDeath");
                creatureVoice.PlayOneShot(dieSFX);
            }

            if (targetPlayer_ == GameNetworkManager.Instance.localPlayerController && currentlyGrabbed) {
                GameNetworkManager.Instance.localPlayerController.transform.position = grabArea.transform.position;
            }
        }
        public override void DoAIInterval() {
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
                    if (FindClosestTargetEnemyInRange(30f)) {
                        // chase the target enemy.
                        // SCREAM
                        StopSearch(currentSearch);
                        previousStateIndex = currentBehaviourStateIndex;
                        nextStateIndex = (int)State.RunningToPrey;
                        nextAnimationName = "startChase";
                        DoAnimationClientRpc("startScream");
                        SwitchToBehaviourClientRpc((int)State.Scream);
                        targettingEnemy = true;
                    } else if (FindClosestPlayerInRange(30f)) {
                        // chase the target player.
                        // SCREAM
                        StopSearch(currentSearch);
                        previousStateIndex = currentBehaviourStateIndex;
                        nextStateIndex = (int)State.RunningToPrey;
                        nextAnimationName = "startChase";
                        DoAnimationClientRpc("startScream");
                        SwitchToBehaviourClientRpc((int)State.Scream);
                        targettingPlayer = true;
                        return;
                    }

                    break;
                case (int)State.RunningToPrey:
                    agent.speed = 20;
                    // Keep targetting target enemy, unless they are over 20 units away and we can't see them.
                    if (targettingEnemy) {
                        if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance && !DWHasLineOfSightToPosition(targetEnemy.transform.position) || targetEnemy == null) {
                            LogIfDebugBuild("Stop chasing target enemy");
                            StartSearch(transform.position);
                            targettingEnemy = false;
                            targetEnemy = null;
                            previousStateIndex = currentBehaviourStateIndex;
                            nextStateIndex = (int)State.SearchingForPrey;
                            nextAnimationName = "startWalk";
                            DoAnimationClientRpc("startScream");
                            SwitchToBehaviourClientRpc((int)State.Scream);
                            return;
                        }
                        SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
                    }
                    else if (targettingPlayer) {
                        if (Vector3.Distance(transform.position, targetPlayer_.transform.position) > seeableDistance && !DWHasLineOfSightToPosition(targetPlayer_.transform.position) || targetPlayer_ == null) {
                            LogIfDebugBuild("Stop chasing target player");
                            StartSearch(transform.position);
                            targettingPlayer = false;
                            targetPlayer_ = null;
                            previousStateIndex = currentBehaviourStateIndex;
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
                        previousStateIndex = currentBehaviourStateIndex;
                        SwitchToBehaviourClientRpc((int)State.Scream);
                    }
                    break;
                case (int)State.SlashingPrey:
                    agent.speed = 0f;
                    
                    if (Vector3.Distance(transform.position, targetEnemy.transform.position) >= 3f && targetEnemy != null) {
                        tooFarAway = true;
                        delayedResponse+= Time.deltaTime;
                        if (delayedResponse >= 3f) {
                            delayedResponse = 0f;
                            previousStateIndex = currentBehaviourStateIndex;
                            DoAnimationClientRpc("startChase");
                            SwitchToBehaviourClientRpc((int)State.RunningToPrey);
                        }
                    }
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
        // Methods that get called through AnimationEvents
        public void ShakePlayerCameraOnDistance() {
                float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
                switch (distance) {
                    case < 19f:
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);

                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        break;
                    case < 25 and >= 20:
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        break;
                    case < 35f and >= 25:
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        break;
                }
        }
        public IEnumerator ScreamPause() {
            creatureVoice.PlayOneShot(screamSound);
            yield return new WaitForSeconds(screamAnimation.length);
            DoAnimationClientRpc(nextAnimationName);
            if (previousStateIndex == (int)State.PlayingWithPrey) {
                StartSearch(transform.position);    
            }
            SwitchToBehaviourClientRpc(nextStateIndex);
            StopCoroutine(ScreamPause());
        }
        public void DriftwoodScream() { // run this multiple times in one scream animation
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= 20f && !player.isInHangarShipRoom) {
                    player.DamagePlayer(5, causeOfDeath: Plugin.RupturedEardrums); // make this damage multiple times through the scream animation
                }
            }
        }
        public void ParticlesFromEatingPrey() {
           // Make some like, red, steaming hot particles come out of the enemy corpses.
           // Also colour the hands a bit red.
        }
        public void PlayRunFootsteps() {
            creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        }
        public void PlayWalkFootsteps() {
            creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        }
        public bool FindClosestTargetEnemyInRange(float range) {
            EnemyAI closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
                if ((enemy.enemyType.enemyName == "MouthDog" || enemy.enemyType.enemyName == "Baboon hawk" || enemy.enemyType.enemyName == "Masked") && DWHasLineOfSightToPosition(enemy.transform.position, 45f, (int)range)) {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < minDistance) {
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

        public bool FindClosestPlayerInRange(float range) {
            PlayerControllerB closestPlayer = null;
            float minDistance = float.MaxValue;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && DWHasLineOfSightToPosition(player.transform.position, 45f, (int)range)) {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance < minDistance) {
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
        public void SlashEnemy() {
            // Do Chain IK stuff later, see PinkGiantAI.cs for reference.
            if (targettingEnemy) {
                if (!tooFarAway) {
                    enemyPositionBeforeDeath = targetEnemy.transform.position;
                    targetEnemy.HitEnemy(1, null, true);
                    if (targetEnemy.enemyHP <= 0) {
                        nextStateIndex = (int)State.EatingPrey;
                        nextAnimationName = "startEating";
                        targettingEnemy = false;
                        targetEnemy = null;
                        ApproachCorpse();
                    }
                }
            } else {
                LogIfDebugBuild("This shouldn't be happening, please report this.");
            }
        }
        public void ApproachCorpse() {
            // TODO: approach the corpse and then feed on it
            SetDestinationToPosition(enemyPositionBeforeDeath - new Vector3(0.3f, 0f, 0.3f), true);
            SwitchToBehaviourClientRpc(nextStateIndex);
            DoAnimationClientRpc(nextAnimationName);
        }
        public void DiggingIntoEnemyBody() { //TODO: Do this through an animation event
            if (numberOfFeedings <= 4) { //TODO: Eating animation should loop
                numberOfFeedings++;
                // TODO: change colour of hand material into a redder colour gradually
            }
            else {
                numberOfFeedings = 0;
                previousStateIndex = currentBehaviourStateIndex;
                DoAnimationClientRpc("startWalk");
                SwitchToBehaviourClientRpc((int)State.SearchingForPrey);
            }
        }
        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
            if (collidedEnemy == targetEnemy && targetEnemy != null && currentBehaviourStateIndex == (int)State.RunningToPrey && !tooFarAway) {
                tooFarAway = false;
                delayedResponse = 0f;
                SwitchToBehaviourClientRpc((int)State.SlashingPrey);
                DoAnimationClientRpc("startSlash");
            }
        }
        public override void OnCollideWithPlayer(Collider other) {
            if (other.GetComponent<PlayerControllerB>() == targetPlayer_ && currentBehaviourStateIndex != (int)State.PlayingWithPrey) {
                playerPositionBeforeGrab = GameNetworkManager.Instance.localPlayerController.transform.position;
                DoAnimationClientRpc("startThrow");
                SwitchToBehaviourClientRpc((int)State.PlayingWithPrey);
            }
        }
        public IEnumerator ThrowPlayer() {
            yield return new WaitForSeconds(throwAnimation.length);
            try {
                LogIfDebugBuild("Setting Kinematics to true");
                targetPlayer_.GetComponent<Rigidbody>().isKinematic = true;
            } catch {
                LogIfDebugBuild("Trying to change kinematics of an unknown player.");
            }
            // Reset targeting
            targettingPlayer = false;
            targetPlayer_ = null;

            
            previousStateIndex = currentBehaviourStateIndex;
            nextStateIndex = (int)State.SearchingForPrey;
            nextAnimationName = "startWalk";
            DoAnimationClientRpc("startScream");
            SwitchToBehaviourClientRpc((int)State.Scream);
            StopCoroutine(ThrowPlayer());
        }
        public void GrabPlayer() {
            currentlyGrabbed = true;
        }
        public void ThrowingPlayer() {
            if (targetPlayer_ == null) {
                LogIfDebugBuild("No player to throw, This is a bug, please report this");
                return;
            }
            currentlyGrabbed = false;
            // GameNetworkManager.Instance.localPlayerController.transform.position = playerPositionBeforeGrab;

            // Calculate the throwing direction with an upward angle
            Vector3 backDirection = transform.TransformDirection(Vector3.back).normalized;
            Vector3 upDirection = transform.TransformDirection(Vector3.up).normalized;
            // Creating a direction that is 60 degrees upwards from the back direction
            Vector3 throwingDirection = (backDirection + Quaternion.AngleAxis(45, transform.right) * upDirection).normalized;

            // Calculate the throwing force
            float throwForceMagnitude = 75;
            // Throw the player
            LogIfDebugBuild("Launching Player");
            GiantPatches.thrownByGiant = true;
            Rigidbody playerBody = targetPlayer_.GetComponent<Rigidbody>();
            targetPlayer_.GetComponent<Rigidbody>().isKinematic = false;
            playerBody.velocity = Vector3.zero; // Reset any existing velocity
            playerBody.AddTorque(Vector3.Cross(throwingDirection, transform.up) * throwForceMagnitude, ForceMode.Impulse);
            playerBody.AddForce(throwingDirection * throwForceMagnitude, ForceMode.Impulse);
        }
        public void DriftwoodGiantSeePlayerEffect() {
            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead || GameNetworkManager.Instance.localPlayerController.isInsideFactory && targetPlayer_ != null && !targettingPlayer) {
                return;
            }
            if (currentBehaviourStateIndex == (int)State.RunningToPrey && targetPlayer_ == GameNetworkManager.Instance.localPlayerController) {
                GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(1.4f);
                return;
            }
            if (!GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && DWHasLineOfSightToPosition(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, 45f, 60)) {
                if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 15f) {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.7f);
                } else {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f);
                }
            }
        }
        public bool DWHasLineOfSightToPosition(Vector3 pos, float width = 45f, int range = 60, float proximityAwareness = 7.5f) {
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
        public IEnumerator SpawnAnimationCooldown() {
            yield return new WaitForSeconds(spawnAnimation.length);
            previousStateIndex = currentBehaviourStateIndex;
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
