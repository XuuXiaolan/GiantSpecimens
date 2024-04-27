using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.Animations.Rigging;
using UnityEngine.AI;
using GiantSpecimens.Patches;
using GiantSpecimens.src;
using GiantSpecimens.Configs;

namespace GiantSpecimens.Enemy;
class DriftwoodGiantAI : EnemyAI, IVisibleThreat {
    #pragma warning disable 0649
    public Collider grabArea;
    public Material handsMaterial;
    public AnimationClip spawnAnimation;
    public AnimationClip screamAnimation;
    public AnimationClip throwAnimation;
    public AnimationClip slashAnimation;
    public ChainIKConstraint RightShoulder;        
    public AudioSource MouthVoice;
    public AudioClip[] stompSounds;
    public AudioClip eatenSound;
    public AudioClip screamSound;
    public AudioClip spawnSound;
    public AudioClip stunSound;
    public AudioClip throwSound;
    public AudioClip slashSound;
    public AudioClip[] hitSound;
    public AudioClip[] walkSounds;
    #pragma warning restore 0649
    [NonSerialized]
    private NetworkVariable<NetworkBehaviourReference> _playerNetVar = new();
    public PlayerControllerB DriftwoodTargetPlayer
    {
        get
        {
            return (PlayerControllerB)_playerNetVar.Value;
        }
        set 
        {
            if (value == null)
            {
                _playerNetVar.Value = null;
            }
            else
            {
                _playerNetVar.Value = new NetworkBehaviourReference(value);
            }
        }
    }
    [NonSerialized]
    public string levelName;
    [NonSerialized]
    public float screamRange;
    [NonSerialized]
    public string[] enemyTargetWhitelist;
    [NonSerialized]
    public EnemyAI targetEnemy;
    [NonSerialized]
    public bool targettingEnemy = false;
    [NonSerialized]
    public Material newHandsMaterial;
    [NonSerialized]
    public float seeableDistance = 50f;
    [NonSerialized]
    public float slashingRange = 6f;
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
    public float rangeOfSight = 50f;
    [NonSerialized]
    public string nextAnimationName = "";
    [NonSerialized]
    public bool testBuild = false; 
    [NonSerialized]
    public int numberOfFeedings = 0; // Number of times the giant has dipped its hand inside the giant.
    [NonSerialized]
    public LineRenderer line; // Debug line that shows destination of movement
    [NonSerialized]
    public float awarenessLevel = 0.0f; // Giant's awareness level of the player
    [NonSerialized]
    public float maxAwarenessLevel = 100.0f; // Maximum awareness level
    [NonSerialized]
    public float awarenessDecreaseRate = 2.5f; // Rate of awareness decrease per second when the player is not seen
    [NonSerialized]
    public float awarenessIncreaseRate = 5.0f; // Base rate of awareness increase when the player is seen
    [NonSerialized]
    public float awarenessIncreaseMultiplier = 6.0f; // Multiplier for awareness increase based on proximity
    [NonSerialized]
    public bool canSlash = true;
    [NonSerialized]
    public Transform shipBoundaries;
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
        SpawnAnimation, // Spawning
        IdleAnimation, // Idling
        SearchingForPrey, // Wandering
        RunningToPrey, // Chasing
        SlashingPrey, // Hitting prey
        EatingPrey, // Eating
        PlayingWithPrey, // Playing with a player's body
        Scream, // Screams and damages player
        Stunned, // Stunned
    }

    void LogIfDebugBuild(string text) {
        #if DEBUG
        Plugin.Logger.LogInfo(text);
        #endif
    }
    public void AdjustMaterialColor(Material material, float redAdjustment) {
        Color originalColor = material.color;
        float newRed = Mathf.Clamp01(originalColor.r + redAdjustment);
        float newGreen = Mathf.Clamp01(originalColor.g - redAdjustment);
        float newBlue = Mathf.Clamp01(originalColor.b - redAdjustment);
        material.color = new Color(newRed, newGreen, newBlue, originalColor.a);
        Debug.Log($"Adjusted Color: {material.color}");  // Log to confirm the color adjustment
    }
    public override void Start() {
        base.Start();
        shipBoundaries = StartOfRound.Instance.shipBounds.transform;
        shipBoundaries.localScale *= 1.1f;
        screamRange = GiantSpecimensConfig.ConfigScreamRange.Value;
        SkinnedMeshRenderer handsRenderer = transform.Find("Body").GetComponent<SkinnedMeshRenderer>();
        if (handsRenderer != null) {
            Material handsMaterial = null;
            for (int i = 0; i < handsRenderer.materials.Length; i++) {
                if (handsRenderer.materials[i].name.Contains("DW-Hands")) {
                    handsMaterial = handsRenderer.materials[i];
                    break; // Exit the loop once you find the material
                }
            } 
            if (handsMaterial != null) {
                // Create a new material based on the found material to ensure it is a unique instance
                newHandsMaterial = new Material(handsMaterial);
                handsRenderer.materials = handsRenderer.materials.Select(m => m.name.Contains("DW-Hands") ? newHandsMaterial : m).ToArray();
            } else {
                LogIfDebugBuild("Material not found");
            }
        } else {
            LogIfDebugBuild("Renderer not found");
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
        enemyTargetWhitelist = ["Baboon hawk", "Butler", "Centipede", "Crawler", "Hoarding bug", "Masked", "Mouthdog", "Nutcracker"];
        creatureVoice.PlayOneShot(spawnSound);
        StartCoroutine(SpawnAnimationCooldown());
        SwitchToBehaviourClientRpc((int)State.SpawnAnimation);
    }
    public override void Update() {
        base.Update();
        if(isEnemyDead) return;
        if (stunNormalizedTimer > 0f && currentBehaviourStateIndex != (int)State.Stunned) {
            StopSearch(currentSearch);
            previousStateIndex = currentBehaviourStateIndex;
            nextStateIndex = (int)State.SearchingForPrey;
            nextAnimationName = "startWalk";
            DoAnimationClientRpc("startStun");
            StartCoroutine(StunPause());
            SwitchToBehaviourClientRpc((int)State.Stunned);
        }
        if (GameNetworkManager.Instance.localPlayerController != null && DWHasLineOfSightToPosition(GameNetworkManager.Instance.localPlayerController.transform.position)) {
            DriftwoodGiantSeePlayerEffect();
        }
        if (currentBehaviourStateIndex == (int)State.SearchingForPrey) {
            UpdateAwareness();
        }
        if (DriftwoodTargetPlayer == GameNetworkManager.Instance.localPlayerController && currentlyGrabbed) {
            GameNetworkManager.Instance.localPlayerController.transform.position = grabArea.transform.position;
        }
    }
    public override void DoAIInterval() {
        if (testBuild) { 
            StartCoroutine(DrawPath(line, agent));
        }
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        switch(currentBehaviourStateIndex) {
            case (int)State.SpawnAnimation:
                agent.speed = 0f;
                break;
            case (int)State.IdleAnimation:
                agent.speed = 0f;
                break;
            case (int)State.SearchingForPrey:
                agent.speed = 5;
                if (FindClosestTargetEnemyInRange(rangeOfSight)) {
                    // chase the target enemy.
                    // SCREAM
                    StopSearch(currentSearch);
                    previousStateIndex = currentBehaviourStateIndex;
                    nextStateIndex = (int)State.RunningToPrey;
                    nextAnimationName = "startChase";
                    DoAnimationClientRpc("startScream");
                    SwitchToBehaviourClientRpc((int)State.Scream);
                    targettingEnemy = true;
                } else if (FindClosestPlayerInRange(rangeOfSight) && awarenessLevel >= 25f) {
                    // chase the target player.
                    // SCREAM
                    StopSearch(currentSearch);
                    previousStateIndex = currentBehaviourStateIndex;
                    nextStateIndex = (int)State.RunningToPrey;
                    nextAnimationName = "startChase";
                    DoAnimationClientRpc("startScream");
                    SwitchToBehaviourClientRpc((int)State.Scream);
                    return;
                }

                break;
            case (int)State.RunningToPrey:
                agent.speed = 20;
                // Keep targetting target enemy, unless they are over 20 units away and we can't see them.
                if (targettingEnemy) {
                    if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance+10f && !DWHasLineOfSightToPosition(targetEnemy.transform.position) || targetEnemy == null) {
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
                else if (DriftwoodTargetPlayer != null) {
                    if (Vector3.Distance(transform.position, DriftwoodTargetPlayer.transform.position) > seeableDistance+10f && !DWHasLineOfSightToPosition(DriftwoodTargetPlayer.transform.position) || Vector3.Distance(DriftwoodTargetPlayer.transform.position, shipBoundaries.position) < 11) {
                        LogIfDebugBuild("Stop chasing target player");
                        StartSearch(transform.position);
                        previousStateIndex = currentBehaviourStateIndex;
                        nextStateIndex = (int)State.SearchingForPrey;
                        nextAnimationName = "startWalk";
                        DoAnimationClientRpc("startScream");
                        SwitchToBehaviourClientRpc((int)State.Scream);
                        return;
                    }
                    SetDestinationToPosition(DriftwoodTargetPlayer.transform.position, checkForPath: true);
                } else {
                    LogIfDebugBuild("If you see this, something went wrong.");
                    LogIfDebugBuild("Resettings state to Scream Animation");
                    previousStateIndex = currentBehaviourStateIndex;
                    SwitchToBehaviourClientRpc((int)State.Scream);
                }
                break;
            case (int)State.SlashingPrey:
                agent.speed = 0f;
                if (targettingEnemy) {
                    float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);
                    if (canSlash && distanceToEnemy < slashingRange + 1.0f) {  // Buffer zone
                        if (distanceToEnemy < slashingRange) {
                            creatureSFX.PlayOneShot(slashSound);
                            DoAnimationClientRpc("startSlash");
                            RightShoulder.data.target = targetEnemy.transform;
                            canSlash = false;
                            StartCoroutine(SlashCooldown());
                        }
                    }
                    else if (distanceToEnemy > slashingRange && distanceToEnemy <= seeableDistance && targetEnemy != null && canSlash) {
                        // Enemy is alive but out of slashing range, reposition
                        previousStateIndex = currentBehaviourStateIndex;
                        nextStateIndex = (int)State.RunningToPrey;
                        DoAnimationClientRpc("startChase");
                        SwitchToBehaviourClientRpc((int)State.RunningToPrey);
                    }
                }
                break;
            case (int)State.EatingPrey:
                break;
            case (int)State.PlayingWithPrey:
                agent.speed = 0f;
                break;
            case (int)State.Scream:
                agent.speed = 0f;
                // Does nothing, on purpose.
                break;
            case (int)State.Stunned:
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
                case < 4f:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);

                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
                case < 15 and >= 5:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
                case < 25f and >= 15:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
            }
    }
    public IEnumerator ScreamPause() {
        yield return new WaitForSeconds(screamAnimation.length);
        DoAnimationClientRpc(nextAnimationName);
        if (previousStateIndex == (int)State.PlayingWithPrey) {
            StartSearch(transform.position);    
        }
        SwitchToBehaviourClientRpc(nextStateIndex);
        StopCoroutine(ScreamPause());
    }
    public IEnumerator StunPause() {
        creatureVoice.PlayOneShot(stunSound);
        yield return new WaitForSeconds(stunNormalizedTimer);
        StartSearch(transform.position);
        DoAnimationClientRpc(nextAnimationName);
        SwitchToBehaviourClientRpc(nextStateIndex);
        StopCoroutine(StunPause());
    }
    public void Screaming() {
        creatureVoice.PlayOneShot(screamSound);
    }
    public void DriftwoodScream() { // run this multiple times in one scream animation
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= screamRange && !player.isInHangarShipRoom) {
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
        creatureVoice.PlayOneShot(walkSounds[UnityEngine.Random.Range(0, walkSounds.Length)]);
    }
    public IEnumerator ThrowPlayer() {
        RightShoulder.data.target = DriftwoodTargetPlayer.transform;
        yield return new WaitForSeconds(throwAnimation.length+2f);
        try {
            LogIfDebugBuild("Setting Kinematics to true");
            DriftwoodTargetPlayer.GetComponent<Rigidbody>().isKinematic = true;
        } catch {
            LogIfDebugBuild("Trying to change kinematics of an unknown player.");
        }
        // Reset targeting
        
        previousStateIndex = currentBehaviourStateIndex;
        nextStateIndex = (int)State.SearchingForPrey;
        nextAnimationName = "startWalk";
        DoAnimationClientRpc("startScream");
        SwitchToBehaviourClientRpc((int)State.Scream);
        StopCoroutine(ThrowPlayer());
    }
    public void GrabPlayer() {
        GiantPatches.grabbedByGiant = true;
        currentlyGrabbed = true;
        RightShoulder.data.target = null;
    }
    public void ThrowingPlayer() {
        if (DriftwoodTargetPlayer == default || DriftwoodTargetPlayer == null) {
            LogIfDebugBuild("No player to throw, This is a bug, please report this");
            return;
        }
        GiantPatches.grabbedByGiant = false;
        currentlyGrabbed = false;
        // GameNetworkManager.Instance.localPlayerController.transform.position = playerPositionBeforeGrab;

        // Calculate the throwing direction with an upward angle
        Vector3 backDirection = transform.TransformDirection(Vector3.back).normalized;
        Vector3 upDirection = transform.TransformDirection(Vector3.up).normalized;
        // Creating a direction that is 45 degrees upwards from the back direction
        Vector3 throwingDirection = (backDirection + Quaternion.AngleAxis(55, transform.right) * upDirection).normalized;

        // Calculate the throwing force
        float throwForceMagnitude = 100;
        // Throw the player
        LogIfDebugBuild("Launching Player");
        GiantPatches.thrownByGiant = true;
        Rigidbody playerBody = DriftwoodTargetPlayer.GetComponent<Rigidbody>();
        DriftwoodTargetPlayer.GetComponent<Rigidbody>().isKinematic = false;
        playerBody.velocity = Vector3.zero; // Reset any existing velocity
        playerBody.AddTorque(Vector3.Cross(throwingDirection, transform.up) * throwForceMagnitude, ForceMode.Impulse);
        playerBody.AddForce(throwingDirection * throwForceMagnitude, ForceMode.Impulse);
    }
    public void SlashEnemy() {
        // Do Chain IK stuff later, see PinkGiantAI.cs for reference.
        if (targettingEnemy) {
            enemyPositionBeforeDeath = targetEnemy.transform.position;
            // Slowly turn towards the target enemy
            Vector3 targetDirection = (targetEnemy.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, transform.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
            
            targetEnemy.HitEnemy(1, null, false, -1);
            if (targetEnemy.enemyHP <= 0) {
                nextStateIndex = (int)State.EatingPrey;
                creatureVoice.PlayOneShot(eatenSound);
                nextAnimationName = "startEating";
                targettingEnemy = false;
                targetEnemy = null;
                ApproachCorpse();
            }
        } else {
            LogIfDebugBuild("This shouldn't be happening, please report this.");
        }
    }
    public void DiggingIntoEnemyBody() {
        numberOfFeedings++;
        // Adjust the color of the new material to be similar to the original but 5% more red
        AdjustMaterialColor(newHandsMaterial, 0.05f); // Adjust red by an additional 5%
        if (numberOfFeedings >= 8) {
            numberOfFeedings = 0;
            previousStateIndex = currentBehaviourStateIndex;
            StartSearch(transform.position);
            DoAnimationClientRpc("startWalk");
            SwitchToBehaviourClientRpc((int)State.SearchingForPrey);
        }
    }
    // Methods that aren't called during AnimationEvents
    public void ApproachCorpse() {
        agent.speed = 3.5f;
        SetDestinationToPosition(enemyPositionBeforeDeath, true);
        transform.LookAt(enemyPositionBeforeDeath);
        SwitchToBehaviourClientRpc(nextStateIndex);
        DoAnimationClientRpc(nextAnimationName);
    }
    public bool FindClosestTargetEnemyInRange(float range) {
        EnemyAI closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            if (enemyTargetWhitelist.Contains(enemy.enemyType.enemyName) && enemy.enemyHP > 0 && DWHasLineOfSightToPosition(enemy.transform.position, 75f, (int)range) && !enemy.isEnemyDead) {
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
            DriftwoodTargetPlayer = closestPlayer;
            return true;
        }
        return false;
    }
    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
        if (isEnemyDead) return;
        if (collidedEnemy == targetEnemy && targetEnemy != null && currentBehaviourStateIndex == (int)State.RunningToPrey && targettingEnemy) {
            SwitchToBehaviourClientRpc((int)State.SlashingPrey);
        }
    }
    public override void OnCollideWithPlayer(Collider other) {
        if (isEnemyDead) return;
        if (other.GetComponent<PlayerControllerB>()) {
            awarenessLevel += 10f;
        }
        if (other.GetComponent<PlayerControllerB>() == DriftwoodTargetPlayer && currentBehaviourStateIndex == (int)State.RunningToPrey && DriftwoodTargetPlayer != null) {
            playerPositionBeforeGrab = GameNetworkManager.Instance.localPlayerController.transform.position;
            creatureSFX.PlayOneShot(throwSound);
            DoAnimationClientRpc("startThrow");
            SwitchToBehaviourClientRpc((int)State.PlayingWithPrey);
        }
    }
    public void DriftwoodGiantSeePlayerEffect() {
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead || GameNetworkManager.Instance.localPlayerController.isInsideFactory && DriftwoodTargetPlayer != default && !(DriftwoodTargetPlayer != null)) {
            return;
        }
        if (currentBehaviourStateIndex == (int)State.RunningToPrey && DriftwoodTargetPlayer == GameNetworkManager.Instance.localPlayerController) {
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
    public bool DWHasLineOfSightToPosition(Vector3 pos, float width = 120f, int range = 50, float proximityAwareness = 10f) {
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
    public void UpdateAwareness() {
        bool playerSeen = false;
        float closestPlayerDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && DWHasLineOfSightToPosition(player.transform.position)) {
                playerSeen = true;
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestPlayerDistance) {
                    closestPlayerDistance = distance;
                }
            }
        }

        if (playerSeen) {
            // Increase awareness more quickly for closer players
            float distanceFactor = Mathf.Clamp01((rangeOfSight - closestPlayerDistance) / rangeOfSight);
            awarenessLevel += awarenessIncreaseRate * Time.deltaTime * (1 + distanceFactor * awarenessIncreaseMultiplier);
            awarenessLevel = Mathf.Min(awarenessLevel, maxAwarenessLevel);
        } else {
            // Decrease awareness over time if no player is seen
            awarenessLevel -= awarenessDecreaseRate * Time.deltaTime;
            awarenessLevel = Mathf.Max(awarenessLevel, 0.0f);
        }
    }
    public bool PlayerInSight() {
        PlayerControllerB[] playerControllerB = StartOfRound.Instance.allPlayerScripts;
        foreach (PlayerControllerB player in playerControllerB) {
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && DWHasLineOfSightToPosition(player.transform.position)) {
                return true;
            }
        }
        return false;
    }
    public IEnumerator SlashCooldown() {
        yield return new WaitForSeconds(1.5f);
        canSlash = true;
        RightShoulder.data.target = null;
        StopCoroutine(SlashCooldown());
    }
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        creatureVoice.PlayOneShot(hitSound[UnityEngine.Random.Range(0, hitSound.Length)]);
        if (force == 6) { 
            RunFarAway();
        }
        enemyHP -= force;
        LogIfDebugBuild("Enemy HP: " + enemyHP);
        if (IsOwner && enemyHP <= 0 && !isEnemyDead) {
            KillEnemyOnOwnerClient();
        }
    }
    public override void KillEnemy(bool destroy = false) {
        base.KillEnemy(destroy);
        SpawnHeartOnDeath(transform.position);
        DoAnimationClientRpc("startDeath");
        creatureVoice.PlayOneShot(dieSFX);
    }
    public void RunFarAway() {
        SetDestinationToPosition(ChooseFarthestNodeFromPosition(this.transform.position, avoidLineOfSight: false).position, true);
    }
    public void SpawnHeartOnDeath(Vector3 position) {
        if (GiantSpecimensConfig.ConfigDriftwoodHeartEnabled.Value) {
            Utils.Instance.SpawnScrapServerRpc("DriftWoodGiant", position);
        }
    }
    [ClientRpc]
    public void DoAnimationClientRpc(string animationName)
    {
        LogIfDebugBuild($"Animation: {animationName}");
        creatureAnimator.SetTrigger(animationName);
    }
}