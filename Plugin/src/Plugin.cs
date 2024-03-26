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
using System.Linq;
using System;

namespace GiantSpecimens {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    public class Plugin : BaseUnityPlugin {
        public static Harmony _harmony;
        public static EnemyType PinkGiant;
        public static Item RedWoodPlushie;
        public static Item Whistle;
        public static GiantSpecimensConfig config { get; private set; } // prevent from accidently overriding the config
        internal static new ManualLogSource Logger;

        private void Awake() {
            Logger = base.Logger;
            Assets.PopulateAssets();
            config = new GiantSpecimensConfig(this.Config); // Create the config with the file from here.

            //Scrap stuff
            Whistle = Assets.MainAssetBundle.LoadAsset<Item>("WhistleObj");
            Utilities.FixMixerGroups(Whistle.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Whistle.spawnPrefab);
            TerminalNode wlTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("PinkGiantTN");
            Items.RegisterScrap(Whistle, 5, LevelTypes.All);
            Items.RegisterShopItem(Whistle, null, null, wlTerminalNode, 100);

            RedWoodPlushie = Assets.MainAssetBundle.LoadAsset<Item>("RedWoodPlushieObj");
            Utilities.FixMixerGroups(RedWoodPlushie.spawnPrefab); 
            NetworkPrefabs.RegisterNetworkPrefab(RedWoodPlushie.spawnPrefab);
            bool scrapEnabledConfig = config.configScrapEnabled.Value;
            if (scrapEnabledConfig) {
                string scrapSpawnRatesConfig = config.configScrapRarity.Value;
                // Initialize dictionaries to hold spawn rates for predefined and custom levels.
                Dictionary<LevelTypes, int> spawnRateByLevelTypeScrap = new Dictionary<LevelTypes, int>();
                Dictionary<string, int> spawnRateByCustomLevelTypeScrap = new Dictionary<string, int>();
                foreach (string entry in scrapSpawnRatesConfig.Split(',').Select(s => s.Trim()))
                {
                    string[] entryParts = entry.Split('@');

                    if (entryParts.Length != 2)
                    {
                        continue;
                    }

                    string name = entryParts[0];
                    int spawnrate;

                    if (!int.TryParse(entryParts[1], out spawnrate))
                    {
                        continue;
                    }

                    if (Enum.TryParse<LevelTypes>(name, true, out LevelTypes levelType))
                    {
                        spawnRateByLevelTypeScrap[levelType] = spawnrate;
                        Plugin.Logger.LogInfo($"Registered spawn rate for level type {levelType} to {spawnrate}");
                    }
                    else
                    {
                        spawnRateByCustomLevelTypeScrap[name] = spawnrate;
                        Plugin.Logger.LogInfo($"Registered spawn rate for custom level type {name} to {spawnrate}");
                    }
                }
                Items.RegisterScrap(RedWoodPlushie, spawnRateByLevelTypeScrap, spawnRateByCustomLevelTypeScrap);
            } else {
                Items.RegisterScrap(RedWoodPlushie, 0, LevelTypes.All);
            }

            
            PinkGiant = Assets.MainAssetBundle.LoadAsset<EnemyType>("PinkGiantObj");
            TerminalNode pgTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("PinkGiantTN");
            TerminalKeyword pgTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("PinkGiantTK");
            
            // Network Prefabs need to be registered first. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            NetworkPrefabs.RegisterNetworkPrefab(PinkGiant.enemyPrefab);
            string spawnratesConfig = config.configSpawnRateEntries.Value;
            // Initialize dictionaries to hold spawn rates for predefined and custom levels.
            Dictionary<LevelTypes, int> spawnRateByLevelType = new Dictionary<LevelTypes, int>();
            Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();

            foreach (string entry in spawnratesConfig.Split(',').Select(s => s.Trim()))
            {
                string[] entryParts = entry.Split('@');

                if (entryParts.Length != 2)
                {
                    continue;
                }

                string name = entryParts[0];
                int spawnrate;

                if (!int.TryParse(entryParts[1], out spawnrate))
                {
                    continue;
                }

                if (Enum.TryParse<LevelTypes>(name, true, out LevelTypes levelType))
                {
                    spawnRateByLevelType[levelType] = spawnrate;
                    Plugin.Logger.LogInfo($"Registered spawn rate for level type {levelType} to {spawnrate}");
                }
                else
                {
                    spawnRateByCustomLevelType[name] = spawnrate;
                    Plugin.Logger.LogInfo($"Registered spawn rate for custom level type {name} to {spawnrate}");
                }
            }

            // Assuming RegisterEnemy is a method that takes the parsed configurations.
            RegisterEnemy(PinkGiant, spawnRateByLevelType, spawnRateByCustomLevelType, pgTerminalNode, pgTerminalKeyword);
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