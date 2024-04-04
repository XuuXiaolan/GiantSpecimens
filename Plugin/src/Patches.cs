using System;
using GameNetcodeStuff;

namespace GiantSpecimens.Patches;

public static class GiantPatches {
    public static bool thrownByGiant = false;
    public static void Init() {
        On.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += PlayerControllerB_PlayerHitGroundEffects;
    }
    private static void PlayerControllerB_PlayerHitGroundEffects(On.GameNetcodeStuff.PlayerControllerB.orig_PlayerHitGroundEffects orig, GameNetcodeStuff.PlayerControllerB self) {
        if (thrownByGiant) {
            self.fallValueUncapped = -41;
            thrownByGiant = false;
        }
        orig(self);
    }
}