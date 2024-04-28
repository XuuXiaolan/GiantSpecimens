using GiantSpecimens.Scrap;
using HarmonyLib;
using MoreShipUpgrades.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

namespace GiantSpecimens.src;
[HarmonyPatch(typeof(StartOfRound))]
internal static class StartOfRoundPatcher {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(StartOfRound.Start))]
    static void RegisterScraps() {
        foreach (var item in Plugin.samplePrefabs.Values) {
            if (!StartOfRound.Instance.allItemsList.itemsList.Contains(item)) {
                StartOfRound.Instance.allItemsList.itemsList.Add(item);
            }
        }
    }
    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    public static void StartOfRound_Start(ref StartOfRound __instance)
    {
        __instance.StartCoroutine(WaitForNetworkObject(__instance, CreateNetworkManager));
    }

    private static IEnumerator WaitForNetworkObject(StartOfRound __instance, Action<StartOfRound> action)
    {
        while (__instance.NetworkObject.IsSpawned == false)
        {
            yield return null;
        }
        action(__instance);
    }

    private static void CreateNetworkManager(StartOfRound __instance)
    {
        Plugin.Logger.LogInfo($"IsServer: {__instance.IsServer}");
        if (__instance.IsServer)
        {
            if (GiantSpecimensUtils.Instance == null)
            {
                GameObject go = new("GiantSpecimensUtils")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                go.AddComponent<GiantSpecimensUtils>();
                go.AddComponent<NetworkObject>();
                go.GetComponent<NetworkObject>().Spawn(false);
                Plugin.Logger.LogInfo("Created GiantSpecimensUtils.");
            }
            else
            {
                Plugin.Logger.LogWarning("GiantSpecimensUtils already exists?");
            }
        }
    }
}