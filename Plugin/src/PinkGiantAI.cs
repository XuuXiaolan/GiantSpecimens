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
class PinkGiantAI : EnemyAI, IVisibleThreat {
    
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
    ThreatType IVisibleThreat.type => ThreatType.ForestGiant;
    int IVisibleThreat.SendSpecialBehaviour(int id) {
        return 0; 
    }
    int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition) {
        return 18;
    }
    int IVisibleThreat.GetInterestLevel() {
        return 0;
    }
    Transform IVisibleThreat.GetThreatLookTransform() {
        return eye;
    }
    Transform IVisibleThreat.GetThreatTransform() {
        return base.transform;
    }
    Vector3 IVisibleThreat.GetThreatVelocity() {
        if (base.IsOwner) {
            return agent.velocity;
        }
        return Vector3.zero;
    }
    float IVisibleThreat.GetVisibility() {
        if (isEnemyDead) {
            return 0f;
        }
        if (agent.velocity.sqrMagnitude > 0f) {
            return 1f;
        }
        return 0.75f;
    }
    enum State {
        SpawnAnimation, // Roaring
        IdleAnimation, // Idling
        SearchingForGiant, // Wandering
        RunningToGiant, // Chasing
        EatingGiant, // Eating
    }

    void LogIfDebugBuild(string text) {
        #if DEBUG
        Plugin.Logger.LogInfo(text);
        #endif
    }
    public override void Start() {
        base.Start();
        destinationRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
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

        SpawnableEnemyWithRarity giantEnemyType = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("ForestGiant"));
        if (giantEnemyType != null && GiantSpecimensConfig.ConfigMultiplierForestkeeper.Value >= 0) {
            giantEnemyType.rarity *= GiantSpecimensConfig.ConfigMultiplierForestkeeper.Value;                
        }
        SpawnableEnemyWithRarity DriftWoodGiant = RoundManager.Instance.currentLevel.OutsideEnemies.Find(x => x.enemyType.enemyName.Equals("DriftWoodGiant"));
        if (DriftWoodGiant != null && GiantSpecimensConfig.ConfigMultiplierDriftwood.Value >= 0) {
            DriftWoodGiant.rarity *= GiantSpecimensConfig.ConfigMultiplierDriftwood.Value;
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
        walkingSpeed = GiantSpecimensConfig.ConfigSpeedRedWood.Value;
        distanceFromShip = GiantSpecimensConfig.ConfigShipDistanceRedWood.Value;
        seeableDistance = GiantSpecimensConfig.ConfigForestDistanceRedWood.Value;
        zeusMode = GiantSpecimensConfig.ConfigZeusMode.Value;
        eatOldBirds = GiantSpecimensConfig.ConfigEatOldBirds.Value;


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
        if (isEnemyDead) {
            return;
        }
    }
    public void LateUpdate() {
        if (currentBehaviourStateIndex == (int)State.EatingGiant && targetEnemy != null) {
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
        }
    }
    public void SearchOrChaseTarget() {
        DoAnimationClientRpc("startWalk");
        LogIfDebugBuild("Start Walking Around");
        StartSearch(ChooseFarthestNodeFromPosition(this.transform.position, avoidLineOfSight: false).position);
        SwitchToBehaviourClientRpc((int)State.SearchingForGiant);
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
            case (int)State.SearchingForGiant:
                agent.speed = walkingSpeed;
                if (FindClosestAliveGiantInRange(seeableDistance)) {
                    DoAnimationClientRpc("startChase");
                    StartCoroutine(ChaseCoolDown());
                    LogIfDebugBuild("Start Target Giant");
                    StopSearch(currentSearch);
                    SwitchToBehaviourClientRpc((int)State.RunningToGiant);
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
            case (int)State.RunningToGiant:
                agent.speed = walkingSpeed * 4;
                // Keep targetting closest Giant, unless they are over 20 units away and we can't see them.
                if (targetEnemy == null) {
                    LogIfDebugBuild("Stop Target Giant");
                    DoAnimationClientRpc("startWalk");
                    StartSearch(ChooseFarthestNodeFromPosition(this.transform.position, avoidLineOfSight: false).position);
                    SwitchToBehaviourClientRpc((int)State.SearchingForGiant);
                    return;
                }
                if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance && !RWHasLineOfSightToPosition(targetEnemy.transform.position) || Vector3.Distance(targetEnemy.transform.position, shipBoundaries.position) <= distanceFromShip) {
                    LogIfDebugBuild("Stop Target Giant");
                    DoAnimationClientRpc("startWalk");
                    StartSearch(ChooseFarthestNodeFromPosition(this.transform.position, avoidLineOfSight: false).position);
                    SwitchToBehaviourClientRpc((int)State.SearchingForGiant);
                    return;
                }
                SetDestinationToPosition(targetEnemy.transform.position, checkForPath: false);
                break;

            case (int)State.EatingGiant:
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
        Transform chosenFoot = (foot == "LeftFoot") ? CollisionFootL.transform : CollisionFootR.transform;
        float distanceFromEnemy = Vector3.Distance(chosenFoot.position, enemy.transform.position);

        // Apply damage based on distance
        if (distanceFromEnemy <= 3f) {
            enemy.HitEnemy(2, null, false, -1);
        } else if (distanceFromEnemy <= 10f) {
            enemy.HitEnemy(1, null, false, -1);
        }

        // Optional: Log the distance and remaining HP for debugging
        // LogIfDebugBuild($"Distance: {distanceFromEnemy} HP: {enemy.enemyHP}");
    }
    public void MayZeusHaveMercy() {
        if (RoundManager.Instance.currentLevel.currentWeather == LevelWeatherType.Stormy && zeusMode || testBuild) {
            // Generate a random offset within a 5-unit radius
            // Vector3 strikePosition = GenerateRandomPositionAround(transform.position, 5, destinationRandom);
            // Perform the lightning strike at the random position
            Vector3 strikePosition = lightningSpots[UnityEngine.Random.Range(0, lightningSpots.Length)].transform.position;
            StormScript.StormyWeatherScript.SpawnLightningBolt(strikePosition);
        }
    }
    public Vector3 GenerateRandomPositionAround(Vector3 center, float radius, System.Random random) {
        // Generate a random angle between 0 and 360 degrees
        double angle = random.NextDouble() * Math.PI * 2;
        // Generate a random distance from the center within the specified radius
        double distance = Math.Sqrt(random.NextDouble()) * radius;

        float x = center.x + (float)(distance * Math.Cos(angle));
        float z = center.z + (float)(distance * Math.Sin(angle));
        // Keep y the same to strike at the ground level
        return new Vector3(x, center.y, z);
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
                player.DamagePlayer(15, causeOfDeath: Plugin.InternalBleed);
            }
        }
        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            if (enemy.enemyType.canDie && enemy.enemyHP > 0 && !enemy.isEnemyDead && enemy.enemyType.enemyName != "RedWoodGiant" && enemy.enemyType.enemyName != "DriftWoodGiant" && enemy.enemyType.enemyName != "ForestGiant") {
                DealEnemyDamageFromShockwave(enemy, "RightFoot");
            }
        }
    }

    private Color HexToColor(string hexCode) {
        if (ColorUtility.TryParseHtmlString(hexCode, out Color color)) {
            return color;
        } else {
            return Color.white; // Default color if parsing fails
        }
    }
    public void ParticlesFromEatingForestKeeper() {
        if (targetEnemy.enemyType.enemyName == "ForestGiant") {
            ForestKeeperParticles.Play(); // Also make them be affected by the world for proper fog stuff?
        } else if (targetEnemy.enemyType.enemyName == "DriftWoodGiant") {
            DriftwoodGiantParticles.Play();
        } else if (targetEnemy.enemyType.enemyName == "RadMech") {
            OldBirdParticles.Play();
        }
        LogIfDebugBuild(targetEnemy.enemyType.enemyName);
        targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
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
    bool FindClosestAliveGiantInRange(float range) {
        EnemyAI closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            string enemyName = enemy.enemyType.enemyName;
            if ((enemyName == "ForestGiant" || enemyName == "DriftWoodGiant") && !enemy.isEnemyDead) {
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
    IEnumerator EatForestKeeper() {
        targetEnemy.SetEnemyStunned(true, 10f);
        targetEnemy.creatureVoice.Stop();
        targetEnemy.creatureSFX.Stop();
        EnemyMouthSource.PlayOneShot(eatenSound, 1);
        DoAnimationClientRpc("eatForestKeeper");
        SwitchToBehaviourClientRpc((int)State.EatingGiant);
        yield return new WaitForSeconds(eating.length);
        try {
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemy.enemyType.enemyName == "RadMech")
                {
                    RadMechAI rad = enemy as RadMechAI;
                    targetEnemy.TryGetComponent(out IVisibleThreat threat);
                    if (threat != null && rad.focusedThreatTransform == threat.GetThreatTransform())
                    {
                        LogIfDebugBuild("Stuff is happening!!");
                        rad.targetedThreatCollider = null;
                        rad.CheckSightForThreat();
                    }
                }
            }
        } catch (Exception e) {
            LogIfDebugBuild("Problem:" + e.ToString());
        }
        DoAnimationClientRpc("startWalk");
        SwitchToBehaviourClientRpc((int)State.SearchingForGiant);
        StopCoroutine(EatForestKeeper());
    }
    public void EatingTargetGiant() {
        if (targetEnemy == null) return;
        ParticlesFromEatingForestKeeper();
        eatingEnemy = false;
        sizeUp = false;
        waitAfterChase = false;
    }
    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)  {
        if (isEnemyDead) return;
        if (collidedEnemy == targetEnemy && !eatingEnemy && (currentBehaviourStateIndex == (int)State.RunningToGiant) && waitAfterChase) {
            eatingEnemy = true;
            if (targetEnemy.enemyType.enemyName == "ForestGiant") {
                targetEnemy.transform.Find("FGiantModelContainer").Find("AnimContainer").Find("metarig").Find("spine").Find("spine.003").Find("shoulder.R").Find("upper_arm.R").Find("forearm.R").Find("hand.R").GetComponent<BoxCollider>().enabled = false;
                targetEnemy.transform.Find("FGiantModelContainer").GetComponent<CapsuleCollider>().enabled = false;   
            }
            if (targetEnemy.enemyType.enemyName == "DriftWoodGiant") {
                targetEnemy.transform.Find("Armature").Find("Main Controller").Find("WristIK.L").Find("WristIK.L_end").Find("WristIK.L_end_end").Find("GrabAreaLeft").GetComponent<BoxCollider>().enabled = false;
                targetEnemy.transform.Find("Armature").Find("Main Controller").Find("WristIK.R").Find("WristIK.R_end").Find("WristIK.R_end_end").Find("GrabAreaRight").GetComponent<BoxCollider>().enabled = false;
            }
            if (targetEnemy.enemyType.enemyName == "RadMech") {
                targetEnemy.GetComponent<BoxCollider>().enabled = false;
                targetEnemy.GetComponentInChildren<BoxCollider>().enabled = false;
            }
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
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (force == 6) {
            enemyHP -= 5;
            
            if (OverrideTargetEnemy(seeableDistance) && currentBehaviourStateIndex == (int)State.SearchingForGiant && eatOldBirds) {
                DoAnimationClientRpc("startChase");
                StartCoroutine(ChaseCoolDown());
                LogIfDebugBuild("Start Target Giant");
                StopSearch(currentSearch);
                SwitchToBehaviourClientRpc((int)State.RunningToGiant);
            }
        } else if (force >= 3) {
            enemyHP -= 2;
        } else if (force >= 1) {
            enemyHP -= 1;
        }
        if (IsOwner && enemyHP <= 0 && !isEnemyDead) {
            KillEnemyOnOwnerClient();
        }
        LogIfDebugBuild(enemyHP.ToString());
    }

    public override void KillEnemy(bool destroy = false) { 
        base.KillEnemy(destroy);
        transform.Find("Armature").Find("Bone.008.L.002").Find("Bone.008.L.002_end").Find("CollisionFootL").GetComponent<BoxCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.008.R.001").Find("Bone.008.R.001_end").Find("CollisionFootR").GetComponent<BoxCollider>().enabled = false;
        DoAnimationClientRpc("startDeath");
        SpawnHeartOnDeath(transform.position);
    }
    public bool OverrideTargetEnemy(float range) {
        EnemyAI closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            string enemyName = enemy.enemyType.enemyName;
            if (enemyName == "RadMech" && !enemy.isEnemyDead) {
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