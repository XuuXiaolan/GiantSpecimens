using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using GameNetcodeStuff;
using MonoMod.Cil;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using GiantSpecimens.Enemy;

namespace GiantSpecimens.Patches;

public static class GiantPatches {
    public static bool thrownByGiant = false;
    public static bool grabbedByGiant = false;
    public static bool addedToDebug = false; // This method of initializing can be changed to your liking.
    public static void Init() {
        On.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += PlayerControllerB_PlayerHitGroundEffects;
        //On.QuickMenuManager.Start += QuickMenuManager_Start;
        //On.Landmine.SpawnExplosion += Landmine_SpawnExplosion;
        //IL.Landmine.SpawnExplosion += Landmine_SpawnExplosion; im really sad i didnt get this working :<
    }

    /*private static void Landmine_SpawnExplosion(ILContext il) {
        var c = new ILCursor(il);
        c.GotoNext(
            x => x.MatchCallvirt<UnityEngine.GameObject>("GetComponentInChildren")
        );
        c.GotoNext(
            MoveType.After,
            x => x.MatchBrfalse(out _)
        );
        c.Previous.OpCode = OpCodes.Brfalse_S;
        c.Emit(OpCodes.Pop);
        // Emit a delegate call that returns a bool, true continues as normal, false skips the damage
        c.EmitDelegate<Func<bool>>(() => {
            return !lightningBeingStruckByRedwood;
        });
        // Emit a brfalse that uses the previously saved label
        c.Emit(OpCodes.Brfalse);
        // Optional: Log the modified IL code for debugging purposes
        Plugin.Logger.LogInfo(il.ToString());
    }*/

    /*private static void Landmine_SpawnExplosion(On.Landmine.orig_SpawnExplosion orig, Vector3 explosionPosition, bool spawnExplosionEffect, float killRange, float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab) {
        if (lightningBeingStruckByRedwood) {
            spawnExplosionEffect = true;
            killRange = 0f;
            damageRange = 4f;
            nonLethalDamage = 5;
        }
        orig(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, overridePrefab);
    }*/

    private static void PlayerControllerB_PlayerHitGroundEffects(On.GameNetcodeStuff.PlayerControllerB.orig_PlayerHitGroundEffects orig, GameNetcodeStuff.PlayerControllerB self) {
        if (thrownByGiant && self != null && self.fallValueUncapped < -39) {
            self.fallValueUncapped = -39;
            thrownByGiant = false;
            self.GetComponent<Rigidbody>().isKinematic = true;
        }
        if (grabbedByGiant && self != null) {
            self.fallValue = 0;
            self.fallValueUncapped = 0;
        }
        orig(self);
    }
    /*private static void QuickMenuManager_Start(On.QuickMenuManager.orig_Start orig, QuickMenuManager self)
    {
        if (addedToDebug)
        {
            orig(self);
            return;
        }
        var testLevel = self.testAllEnemiesLevel;
        var inside = testLevel.Enemies;
        var daytime = testLevel.DaytimeEnemies;
        var outside = testLevel.OutsideEnemies;
        foreach (SpawnableEnemyWithRarity spawnableEnemy in RoundManager.Instance.currentLevel.OutsideEnemies) {
            if (spawnableEnemy.enemyType.enemyName == "RedWoodGiant" || spawnableEnemy.enemyType.enemyName == "DriftWoodGiant") {
                if (!outside.Any(x => x.enemyType == spawnableEnemy.enemyType)) {
                    outside.Add(spawnableEnemy);
                    inside.Remove(spawnableEnemy);
                }
            }
            Plugin.Logger.LogInfo($"Added {spawnableEnemy.enemyType.enemyName} to DebugList [{spawnableEnemy.enemyType.isOutsideEnemy}]");
        }
        addedToDebug = true;
        orig(self);
    }*/
}

