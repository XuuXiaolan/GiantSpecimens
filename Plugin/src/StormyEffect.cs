using System;
using System.Collections;
using DigitalRuby.ThunderAndLightning;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace GiantSpecimens.StormScript;
public class StormyWeatherScript {
    public static void LogIfDebugBuild(string text) {
        #if DEBUG
        Plugin.Logger.LogInfo(text);
        #endif
    }

    public static void SpawnLightningBolt(Vector3 strikePosition)
    {
        System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        Vector3 offset = new Vector3((float)random.Next(-32, 32), 0f, (float)random.Next(-32, 32));
        Vector3 vector = strikePosition + Vector3.up * 160f + offset;

        StormyWeather stormy = UnityEngine.Object.FindObjectOfType<StormyWeather>(true);
        if (stormy == null)
        {
            LogIfDebugBuild("StormyWeather not found");
            return;
        }

        LogIfDebugBuild($"{vector} -> {strikePosition}");

        LightningBoltPrefabScript localLightningBoltPrefabScript = Object.Instantiate(stormy.targetedThunder);
        localLightningBoltPrefabScript.enabled = true;

        if (localLightningBoltPrefabScript == null)
        {
            LogIfDebugBuild("localLightningBoltPrefabScript not found");
            return;
        }

        localLightningBoltPrefabScript.GlowWidthMultiplier = 2.5f;
        localLightningBoltPrefabScript.DurationRange = new RangeOfFloats { Minimum = 0.6f, Maximum = 1.2f };
        localLightningBoltPrefabScript.TrunkWidthRange = new RangeOfFloats { Minimum = 0.6f, Maximum = 1.2f };
        localLightningBoltPrefabScript.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        localLightningBoltPrefabScript.Source.transform.position = vector;
        localLightningBoltPrefabScript.Destination.transform.position = strikePosition;
        localLightningBoltPrefabScript.AutomaticModeSeconds = 0.2f;
        localLightningBoltPrefabScript.Generations = 8;
        MonoBehaviour host = localLightningBoltPrefabScript as MonoBehaviour;
        host.StartCoroutine(ChangeLightningColor(localLightningBoltPrefabScript));
        localLightningBoltPrefabScript.CreateLightningBoltsNow();

        AudioSource audioSource = Object.Instantiate(stormy.targetedStrikeAudio);
        audioSource.transform.position = strikePosition + Vector3.up * 0.5f;
        audioSource.enabled = true;
        audioSource.volume /= 4.0f;
        stormy.PlayThunderEffects(strikePosition, audioSource);
    }
    private static IEnumerator ChangeLightningColor(LightningBoltPrefabScript boltScript)
    {
        float duration = 1.0f; // Duration of the color change in seconds
        float timer = 0f;
        while (timer < duration)
        {
            float progress = timer / duration;
            boltScript.GlowTintColor = Color.Lerp(new Color(0.8f, 0f, 0f, 0.25f), Color.blue, progress);
            boltScript.LightningTintColor = Color.Lerp(new Color(0.7f, 0f, 0f, 0.25f), Color.cyan, progress);
            boltScript.MainTrunkTintColor = Color.Lerp(new Color(0.6f, 0f, 0f, 0.25f), Color.white, progress);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
