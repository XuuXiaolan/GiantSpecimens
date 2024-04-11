using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace GiantSpecimens.Patches;

public static class GiantPatches {
    public static bool thrownByGiant = false;
    public static Vector3 newExplosionPosition;

    public static void Init() {
        On.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += PlayerControllerB_PlayerHitGroundEffects;
    }

    private static void PlayerControllerB_PlayerHitGroundEffects(On.GameNetcodeStuff.PlayerControllerB.orig_PlayerHitGroundEffects orig, GameNetcodeStuff.PlayerControllerB self) {
        if (thrownByGiant && self != null && self.fallValueUncapped < -41) {
            self.fallValueUncapped = -41;
            thrownByGiant = false;
        }
        orig(self);
    }
}