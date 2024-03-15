using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using BepInEx.Logging;
using System.IO;
using System.Collections.Generic;

namespace GiantSpecimens {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    public class Plugin : BaseUnityPlugin {
        public static Harmony _harmony;
        public static EnemyType PinkGiant;
        public static GiantSpecimensConfig config { get; private set; } // prevent from accidently overriding the config
        internal static new ManualLogSource Logger;

        private void Awake() {
            Logger = base.Logger;
            Assets.PopulateAssets();
            config = new GiantSpecimensConfig(this.Config); // Create the config with the file from here.
            
            PinkGiant = Assets.MainAssetBundle.LoadAsset<EnemyType>("PinkGiantObj");
            var tlTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("PinkGiantTN");
            var tlTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("PinkGiantTK");
            
            // Network Prefabs need to be registered first. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            NetworkPrefabs.RegisterNetworkPrefab(PinkGiant.enemyPrefab);
            int ExperimentationSpawnrate = config.configExperimentationSpawnrateRedWood.Value;
            int AssuranceSpawnrate = config.configAssuranceSpawnrateRedWood.Value;
            int VowSpawnrate = config.configVowSpawnrateRedWood.Value;
            int OffenseSpawnrate = config.configOffenseSpawnrateRedWood.Value;
            int MarchSpawnrate = config.configMarchSpawnrateRedWood.Value;
            int RendSpawnrate = config.configRendSpawnrateRedWood.Value;
            int DineSpawnrate = config.configDineSpawnrateRedWood.Value;
            int TitanSpawnrate = config.configTitanSpawnrateRedWood.Value;
            int ModdedSpawnrate = config.configModdedSpawnrateRedWood.Value;

            Dictionary<LevelTypes, int> spawnRateByLevelType = new() {
                { LevelTypes.ExperimentationLevel, ExperimentationSpawnrate},
                { LevelTypes.AssuranceLevel, AssuranceSpawnrate},
                { LevelTypes.VowLevel, VowSpawnrate},
                { LevelTypes.OffenseLevel, OffenseSpawnrate},
                { LevelTypes.MarchLevel, MarchSpawnrate},
                { LevelTypes.RendLevel, RendSpawnrate},
                { LevelTypes.DineLevel, DineSpawnrate},
                { LevelTypes.TitanLevel, TitanSpawnrate},
                { LevelTypes.Modded, ModdedSpawnrate}
            };
            Dictionary<string, int> spawnRateByCustomLevelType = new () {
                {"EGyptLevel", 30},
            };
            RegisterEnemy(PinkGiant, spawnRateByLevelType, spawnRateByCustomLevelType, tlTerminalNode, tlTerminalKeyword);

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Required by https://github.com/EvaisaDev/UnityNetcodePatcher
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }

    public static class Assets {
        public static AssetBundle MainAssetBundle = null;
        public static void PopulateAssets() {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "pinkgiantassets"));
            if (MainAssetBundle == null) {
                Plugin.Logger.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}