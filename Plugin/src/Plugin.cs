using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using static LethalLib.Modules.Items;
using BepInEx.Logging;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using static BepInEx.BepInDependency;
using GiantSpecimens.Dependency;
using GiantSpecimens.Configs;
using GiantSpecimens.Enemy;
using GiantSpecimens.Patches;
using BepInEx.Bootstrap;
using MoreShipUpgrades;

namespace GiantSpecimens {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    [BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("MaxWasUnavailable.LethalModDataLib", Flags:BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("malco.Lategame_Upgrades", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin {
        public static Harmony _harmony;
        public static EnemyType PinkGiant;
        public static EnemyType DriftGiant;
        public static Item RedWoodPlushie;
        public static Item Whistle;
        public static GiantSpecimensConfig ModConfig { get; private set; } // prevent from accidently overriding the config
        internal static new ManualLogSource Logger;
        public static CauseOfDeath RupturedEardrums = EnumUtils.Create<CauseOfDeath>("RupturedEardrums");
        public static CauseOfDeath InternalBleed = EnumUtils.Create<CauseOfDeath>("InternalBleed");
        public static CauseOfDeath Thwomped = EnumUtils.Create<CauseOfDeath>("Thwomped");
        public static ThreatType DriftwoodGiant = EnumUtils.Create<ThreatType>("DriftwoodGiant");
        // add the causes of death here
        private void Awake() {
            Logger = base.Logger;
            // Lobby Compatibility stuff
            if (LobbyCompatibilityChecker.Enabled) {
                LobbyCompatibilityChecker.Init();
            }
            Assets.PopulateAssets();

            ModConfig = new GiantSpecimensConfig(this.Config); // Create the config with the file from here.
            // Whistle Item/Scrap
            Whistle = Assets.MainAssetBundle.LoadAsset<Item>("WhistleObj");
            Utilities.FixMixerGroups(Whistle.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Whistle.spawnPrefab);
            TerminalNode wlTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("wlTerminalNode");
            RegisterShopItemWithConfig(ModConfig.ConfigWhistleEnabled.Value, ModConfig.ConfigWhistleScrapEnabled.Value, Whistle, wlTerminalNode, ModConfig.ConfigWhistleCost.Value, ModConfig.ConfigWhistleRarity.Value);

            // Redwood Plushie Scrap
            RedWoodPlushie = Assets.MainAssetBundle.LoadAsset<Item>("RedWoodPlushieObj");
            Utilities.FixMixerGroups(RedWoodPlushie.spawnPrefab); 
            NetworkPrefabs.RegisterNetworkPrefab(RedWoodPlushie.spawnPrefab);
            RegisterScrapWithConfig(ModConfig.ConfigRedwoodPlushieEnabled.Value, ModConfig.ConfigRedwoodPlushieRarity.Value, RedWoodPlushie);

            if (Chainloader.PluginInfos.ContainsKey("com.malco.lethalcompany.moreshipupgrades")) {
                MoreShipUpgrades.API.HunterSamples.RegisterSampleItem()
            } else {
                // MyHeartDropRegister();
            }

            // Redwood Giant Enemy
            PinkGiant = Assets.MainAssetBundle.LoadAsset<EnemyType>("PinkGiantObj");
            TerminalNode pgTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("PinkGiantTN");
            TerminalKeyword pgTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("PinkGiantTK");
            NetworkPrefabs.RegisterNetworkPrefab(PinkGiant.enemyPrefab);
            RegisterEnemyWithConfig(ModConfig.ConfigRedWoodEnabled.Value, ModConfig.ConfigRedWoodRarity.Value, PinkGiant, pgTerminalNode, pgTerminalKeyword);

            // Driftwood Giant Enemy
            DriftGiant = Assets.MainAssetBundle.LoadAsset<EnemyType>("DriftwoodGiantObj");
            TerminalNode dgTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("DriftwoodGiantTN");
            TerminalKeyword dgTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("DriftwoodGiantTK");
            NetworkPrefabs.RegisterNetworkPrefab(DriftGiant.enemyPrefab);
            RegisterEnemyWithConfig(ModConfig.ConfigDriftWoodEnabled.Value, ModConfig.ConfigDriftWoodRarity.Value, DriftGiant, dgTerminalNode, dgTerminalKeyword);
            
            GiantPatches.Init();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Required by https://github.com/EvaisaDev/UnityNetcodePatcher
            InitializeNetworkBehaviours();
        }
        private void RegisterEnemyWithConfig(bool enabled, string configMoonRarity, EnemyType enemy, TerminalNode terminalNode, TerminalKeyword terminalKeyword) {
            if (enabled) { 
                (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
                RegisterEnemy(enemy, spawnRateByLevelType, spawnRateByCustomLevelType, terminalNode, terminalKeyword);
                return;
            } else {
                RegisterEnemy(enemy, 0, LevelTypes.All, terminalNode, terminalKeyword);
                return;
            }
        }
        private void RegisterScrapWithConfig(bool enabled, string configMoonRarity, Item scrap) {
            if (enabled) { 
                (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
                RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
            } else {
                RegisterScrap(scrap, 0, LevelTypes.All);
            }
            return;
        }
        private void RegisterShopItemWithConfig(bool enabledShopItem, bool enabledScrap, Item item, TerminalNode terminalNode, int itemCost, string configMoonRarity) {
            if (enabledShopItem) { 
                RegisterShopItem(item, null, null, terminalNode, itemCost);
            }
            if (enabledScrap) {
                RegisterScrapWithConfig(true, configMoonRarity, item);
            }
            return;
        }
        private (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ConfigParsing(string configMoonRarity) {
            Dictionary<LevelTypes, int> spawnRateByLevelType = new Dictionary<LevelTypes, int>();
            Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();

            foreach (string entry in configMoonRarity.Split(',').Select(s => s.Trim())) {
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
            return (spawnRateByLevelType, spawnRateByCustomLevelType);
        }
        private void InitializeNetworkBehaviours() {
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

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "giantspecimenassets"));
            if (MainAssetBundle == null) {
                Plugin.Logger.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}