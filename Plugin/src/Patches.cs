using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace GiantSpecimens.Patches;

public static class GiantPatches {
    public static bool thrownByGiant = false;
    public static bool lightningBeingStruckByRedwood = false;
    public static void Init() {
        On.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += PlayerControllerB_PlayerHitGroundEffects;
        On.Landmine.SpawnExplosion += Landmine_SpawnExplosion;
    }

    private static void Landmine_SpawnExplosion(On.Landmine.orig_SpawnExplosion orig, Vector3 explosionPosition, bool spawnExplosionEffect, float killRange, float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab) {
        if (lightningBeingStruckByRedwood) {
            spawnExplosionEffect = true;
            killRange = 0f;
            damageRange = 4f;
            nonLethalDamage = 5;
        }
        orig(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, overridePrefab);
    }

    private static void PlayerControllerB_PlayerHitGroundEffects(On.GameNetcodeStuff.PlayerControllerB.orig_PlayerHitGroundEffects orig, GameNetcodeStuff.PlayerControllerB self) {
        if (thrownByGiant && self != null && self.fallValueUncapped < -41) {
            self.fallValueUncapped = -41;
            thrownByGiant = false;
        }
        orig(self);
    }
}