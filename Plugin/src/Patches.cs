using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace GiantSpecimens.Patches;

public static class GiantPatches {
    public static bool thrownByGiant = false;
    /*public static NavMeshBuildSource BoxSource10x10() {
        Collider shipBounds = StartOfRound.Instance.shipBounds;
        MeshFilter meshFilter = shipBounds.GetComponent<MeshFilter>();

        var src = new NavMeshBuildSource {
            transform = Matrix4x4.TRS(shipBounds.transform.position, shipBounds.transform.rotation, shipBounds.transform.localScale),
            shape = NavMeshBuildSourceShape.Mesh,
            sourceObject = meshFilter.sharedMesh,
            area = 0 // Default walkable area?
        };

        return src;
    }*/
    public static void Init() {
        On.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += PlayerControllerB_PlayerHitGroundEffects;
        // On.RoundManager.Awake += RoundManager_Awake;
    }

    /*private static void RoundManager_Awake(On.RoundManager.orig_Awake orig, RoundManager self) {
        orig(self);
            // Create a list of NavMeshBuildSource objects
        List<NavMeshBuildSource> sources =
        [
            BoxSource10x10(),
        ];


        // Define the area in which you want the NavMesh to be built
        // This should be large enough to encompass all the sources you're including
        Bounds bounds = new Bounds(StartOfRound.Instance.shipBounds.transform.position, StartOfRound.Instance.shipBounds.transform.localScale);

        // Get the default NavMesh build settings
        var buildSettings = NavMesh.GetSettingsByID(0);

        // Use NavMeshBuilder to bake the NavMesh data
        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(buildSettings, sources, bounds, StartOfRound.Instance.shipBounds.transform.position, Quaternion.identity);

        // If baking was successful, add the NavMeshData to the scene
        if (navMeshData != null) {
            // It's often a good idea to keep a reference to the instance
            // so you can remove it later if you need to rebake the NavMesh
            NavMeshDataInstance instance = NavMesh.AddNavMeshData(navMeshData);
        } else {
            Debug.LogError("Failed to bake NavMesh.");
        }
    }*/

    private static void PlayerControllerB_PlayerHitGroundEffects(On.GameNetcodeStuff.PlayerControllerB.orig_PlayerHitGroundEffects orig, GameNetcodeStuff.PlayerControllerB self) {
        if (thrownByGiant) {
            self.fallValueUncapped = -41;
            thrownByGiant = false;
        }
        orig(self);
    }
}