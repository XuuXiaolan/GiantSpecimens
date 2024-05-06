using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using UnityEngine.Animations.Rigging;
using System.Text.RegularExpressions;
using UnityEngine.AI;
using GiantSpecimens.Colours;
using GiantSpecimens.Patches;
using GiantSpecimens.Scrap;
using GiantSpecimens.src;
using GiantSpecimens.Configs;
using System.IO;

namespace GiantSpecimens.Enemy;
class StellarSovereignAI : EnemyAI {
    
    // We set these in our Asset Bundle, so we can disable warning CS0649:
    // Field 'field' is never assigned to, and will always have its default value 'value'
    #pragma warning disable 0649
    public Collider AttackArea;
    public ParticleSystem DustParticlesLeft;
    public ParticleSystem DustParticlesRight;
    public ParticleSystem ForestKeeperParticles;
    public ParticleSystem DriftwoodGiantParticles;
    public ParticleSystem OldBirdParticles;
    public ParticleSystem DeathParticles;
    public Collider CollisionFootR;
    public Collider CollisionFootL;
    public ChainIKConstraint LeftFoot;
    public ChainIKConstraint RightFoot;
    public AudioSource FootSource;
    public AudioSource EnemyMouthSource;
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
    public GameObject[] lightningSpots;
    public GameObject eatingArea;
    #pragma warning restore 0649
    [NonSerialized]
    public bool sizeUp = false;
    [NonSerialized]
    public static LevelColorMapper levelColorMapper = new();
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
    public bool eatOldBirds;
    [NonSerialized]
    public bool zeusMode;
    [NonSerialized]
    public float distanceFromEnemy;
    [NonSerialized]
    public Transform shipBoundaries;
    [NonSerialized]
    public Vector3 midpoint;
    [NonSerialized]
    public bool testBuild = false; 
    [NonSerialized]
    public LineRenderer line;
    [NonSerialized]
    public System.Random destinationRandom;
    [NonSerialized]
    public bool canMove = true;

    enum State {
        SpawnAnimation, // Roaring
        IdleAnimation, // Idling
        Wandering, // Wandering
        AtlasMode,
        Exhausted,
        Crying,
    }

    void LogIfDebugBuild(string text) {
        #if DEBUG
        Plugin.Logger.LogInfo(text);
        #endif
    }
    public override void Start() {
        base.Start();
        destinationRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 22);
        levelName = RoundManager.Instance.currentLevel.name;
        
        LogIfDebugBuild(levelName);
        shipBoundaries = StartOfRound.Instance.shipBounds.transform;
        shipBoundaries.localScale *= 1.5f;
        
        Color dustColor = Color.grey; // Default to grey if no color found
        string footstepColourValue = GiantSpecimensConfig.ConfigColourHexcode.Value;
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

        SpawnableEnemyWithRarity StellarSovereign = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("StellarSovereign"));
        if (StellarSovereign != null) {
        LogIfDebugBuild(StellarSovereign.rarity.ToString());
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

        // LogIfDebugBuild(giantEnemyType.rarity.ToString());
        LogIfDebugBuild("The Stellar Sovereign has descended from above...");
        if (testBuild) {
            #if DEBUG
            line = gameObject.AddComponent<LineRenderer>();
            line.widthMultiplier = 0.2f; // reduce width of the line
            #endif
        }
        FootSource.pitch *= 0.5f;
        EnemyMouthSource.pitch *= 0.5f;

        FootSource.PlayOneShot(spawnSound);
        EnemyMouthSource.PlayOneShot(roarSound);
        StartCoroutine(ScalingUp());
        SwitchToBehaviourClientRpc((int)State.SpawnAnimation);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) {
            return;
        }
    }
    public void SearchOrChaseTarget() {
        DoAnimationClientRpc("startWalk");
        LogIfDebugBuild("Start Walking Around");
        StartSearch(transform.position);
        SwitchToBehaviourClientRpc((int)State.Wandering);
    }
    public override void DoAIInterval() {
        if (testBuild) { 
            StartCoroutine(DrawPath(line, agent));
        }
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
            return;
        }
        switch(currentBehaviourStateIndex) {
            case (int)State.SpawnAnimation:
                agent.speed = 0f;
                break;
            case (int)State.IdleAnimation:
                agent.speed = 0f;
                break;
            case (int)State.Wandering:
                agent.speed = 6f;
                if (false) {
                    DoAnimationClientRpc("startChase");
                    LogIfDebugBuild("Prepare to stop the meteorite...");
                    StopSearch(currentSearch);
                    SwitchToBehaviourClientRpc((int)State.AtlasMode);
                } // Look for Forest Keeper
                break;
            case (int)State.AtlasMode:
                agent.speed = 0f;
                // condition that checks if the giant is in the right place, switch animation, and wait until the meteor is gone.
                break;

            case (int)State.Exhausted:
                agent.speed = 0f;
                // state after atlas mode.
                break;
            case (int)State.Crying:
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
    public void DealEnemyDamageFromShockwave(EnemyAI enemy, string foot) {
        Transform chosenFoot = (foot == "LeftFoot") ? CollisionFootL.transform : CollisionFootR.transform;
        float distanceFromEnemy = Vector3.Distance(chosenFoot.position, enemy.transform.position);

        // Apply damage based on distance
        if (distanceFromEnemy <= 3f) {
            enemy.HitEnemy(4, null, false, -1);
        } else if (distanceFromEnemy <= 10f) {
            enemy.HitEnemy(2, null, false, -1);
        }

        // Optional: Log the distance and remaining HP for debugging
        // LogIfDebugBuild($"Distance: {distanceFromEnemy} HP: {enemy.enemyHP}");
    }
    public void LeftFootStepInteractions() {
        DustParticlesLeft.Play(); // Play the particle system with the updated color
        FootSource.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
            float distance = Vector3.Distance(CollisionFootL.transform.position, player.transform.position);
            if (distance <= 10f && !player.isInHangarShipRoom) {
                player.DamagePlayer(30, causeOfDeath: Plugin.InternalBleed);
            }
        }
        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            if (enemy.enemyType.canDie && enemy.enemyHP > 0 && !enemy.isEnemyDead && enemy.enemyType.enemyName != "RedWoodGiant" && enemy.enemyType.enemyName != "DriftWoodGiant" && enemy.enemyType.enemyName != "ForestGiant") {
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
                player.DamagePlayer(30, causeOfDeath: Plugin.InternalBleed);
            }
        }
        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            if (enemy.enemyType.canDie && enemy.enemyHP > 0 && !enemy.isEnemyDead && enemy.enemyType.enemyName != "RedWoodGiant" && enemy.enemyType.enemyName != "DriftWoodGiant" && enemy.enemyType.enemyName != "ForestGiant") {
                DealEnemyDamageFromShockwave(enemy, "RightFoot");
            }
        }
    } // todo: adding the crying animation and particles, fix how far it can move by maybe using furthest node.

    private Color HexToColor(string hexCode) {
        if (ColorUtility.TryParseHtmlString(hexCode, out Color color)) {
            return color;
        } else {
            return Color.white; // Default color if parsing fails
        }
    }
    
    public void ShakePlayerCamera() {
            float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
            switch (distance) {
                case < 30f:
                
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);

                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
                case < 40 and >= 30:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
                case < 70f and >= 40:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
            }
    }
    IEnumerator ScalingUp() {
        Vector3 newScale = transform.localScale;
        newScale.x *= 0.1f;
        newScale.y *= 0.1f;
        newScale.z *= 0.1f;
        transform.localScale = newScale;

        const float AnimationDuration = 10f; // measured in seconds
        float elapsedTime = 0;

        // Define start and end scales for x, y, and z
        float startXScale = 0.1f;
        float endXScale = 2.5f;
        float startYScale = 0.1f;
        float endYScale = 3.2f;
        float startZScale = 0.1f;
        float endZScale = 2.5f;

        while (elapsedTime < AnimationDuration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
            float lerpFactor = Mathf.Clamp01(elapsedTime / AnimationDuration);

            // Interpolate scale for each axis independently
            float currentXScale = Mathf.Lerp(startXScale, endXScale, lerpFactor);
            float currentYScale = Mathf.Lerp(startYScale, endYScale, lerpFactor);
            float currentZScale = Mathf.Lerp(startZScale, endZScale, lerpFactor);

            transform.localScale = new Vector3(currentXScale, currentYScale, currentZScale);
        }
    }

    public bool SSHasLineOfSightToPosition(Vector3 pos, float width = 45f, int range = 60, float proximityAwareness = -1f) {
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
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (IsOwner && enemyHP <= 0 && !isEnemyDead) {
            KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false) { 
        base.KillEnemy(destroy);
        transform.Find("Armature").Find("Bone.008.L.002").Find("Bone.008.L.002_end").Find("CollisionFootL").GetComponent<BoxCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.008.R.001").Find("Bone.008.R.001_end").Find("CollisionFootR").GetComponent<BoxCollider>().enabled = false;
        DoAnimationClientRpc("startDeath");
        SpawnHeartOnDeath(transform.position);
    }

    public void EnableDeathColliders() {
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone").Find("DeathColliderChest").GetComponent<CapsuleCollider>().enabled = true;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.L").Find("Bone.007.L").Find("DeathColliderLeftHip").GetComponent<CapsuleCollider>().enabled = true;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.L").Find("Bone.007.L").Find("Bone.008.L").Find("DeathColliderLeftLeg").GetComponent<CapsuleCollider>().enabled = true;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.L").Find("Bone.007.L").Find("Bone.008.L").Find("DeathColliderLeftLeg").GetComponent<BoxCollider>().enabled = true;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.R").Find("Bone.007.R").Find("DeathColliderRightHip").GetComponent<CapsuleCollider>().enabled = true;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.R").Find("Bone.007.R").Find("Bone.008.R").Find("DeathColliderRightLeg").GetComponent<CapsuleCollider>().enabled = true;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.R").Find("Bone.007.R").Find("Bone.008.R").Find("DeathColliderRightLeg").GetComponent<BoxCollider>().enabled = true;
    }
    public void DisableDeathColliders() {
        DeathParticles.Play();
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone").Find("DeathColliderChest").GetComponent<CapsuleCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.L").Find("Bone.007.L").Find("DeathColliderLeftHip").GetComponent<CapsuleCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.L").Find("Bone.007.L").Find("Bone.008.L").Find("DeathColliderLeftLeg").GetComponent<CapsuleCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.L").Find("Bone.007.L").Find("Bone.008.L").Find("DeathColliderLeftLeg").GetComponent<BoxCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.R").Find("Bone.007.R").Find("DeathColliderRightHip").GetComponent<CapsuleCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.R").Find("Bone.007.R").Find("Bone.008.R").Find("DeathColliderRightLeg").GetComponent<CapsuleCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.006.L.001").Find("Bone.006.R").Find("Bone.007.R").Find("Bone.008.R").Find("DeathColliderRightLeg").GetComponent<BoxCollider>().enabled = false;
    }
    public void SpawnHeartOnDeath(Vector3 position) {
        if (GiantSpecimensConfig.ConfigRedwoodHeartEnabled.Value && IsHost && !Plugin.LGULoaded) {
            GiantSpecimensUtils.Instance.SpawnScrapServerRpc("RedWoodGiant", position);
        }
    }
    [ClientRpc]
    public void DoAnimationClientRpc(string animationName)
    {
        LogIfDebugBuild($"Animation: {animationName}");
        creatureAnimator.SetTrigger(animationName);
    }
}