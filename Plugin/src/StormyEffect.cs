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
        Vector3 vector = strikePosition + Vector3.up * 160f + new Vector3((float)random.Next(-32, 32), 0f, (float)random.Next(-32, 32));

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

        // Change the color of the lightning bolt material to green
        if (localLightningBoltPrefabScript.LightningMaterialMesh != null) {
            Color green = new Color(1.0f, 0f, 0.0f, 1.0f);
            localLightningBoltPrefabScript.LightningMaterialMesh.color = green;
        } else {
            LogIfDebugBuild("LightningMaterial not found on prefab");
        }

        localLightningBoltPrefabScript.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        localLightningBoltPrefabScript.AutomaticModeSeconds = 0.2f;
        localLightningBoltPrefabScript.Source.transform.position = vector;
        localLightningBoltPrefabScript.Destination.transform.position = strikePosition;
        localLightningBoltPrefabScript.CreateLightningBoltsNow();

        AudioSource audioSource = Object.Instantiate(stormy.targetedStrikeAudio);
        audioSource.transform.position = strikePosition + Vector3.up * 0.5f;
        audioSource.enabled = true;

        stormy.PlayThunderEffects(strikePosition, audioSource);
    }
}
