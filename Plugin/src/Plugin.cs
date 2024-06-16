using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using GiantSpecimens.Dependency;
using GiantSpecimens.Configs;
using GiantSpecimens.Patches;
using GiantSpecimens.src;
using BepInEx.Bootstrap;
using GiantSpecimens.Scrap;
using MoreShipUpgrades.Misc;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using static LethalLib.Modules.Items;
using MoreShipUpgrades.Managers;
using LethalLib.Modules;

namespace GiantSpecimens;
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("MegaPiggy.EnumUtils", Flags:BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(LethalLib.Plugin.ModGUID)] 
[BepInDependency(MoreShipUpgrades.PluginInfo.PLUGIN_GUID, Flags: BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    public static Harmony _harmony = new(PluginInfo.PLUGIN_GUID);
    public static EnemyType PinkGiant;
    public static EnemyType StellarSovereign;
    public static EnemyType DriftGiant;
    public static Item RedWoodPlushie;
    public static Item Whistle;
    public static Item RedWoodHeart;
    public static Item DriftwoodSample;
    public static GiantSpecimensConfig ModConfig { get; private set; } // prevent from accidently overriding the config
    internal static new ManualLogSource Logger;
    public static CauseOfDeath RupturedEardrums = EnumUtils.Create<CauseOfDeath>("RupturedEardrums");
    public static CauseOfDeath InternalBleed = EnumUtils.Create<CauseOfDeath>("InternalBleed");
    public static CauseOfDeath Thwomped = EnumUtils.Create<CauseOfDeath>("Thwomped");
    public static ThreatType DriftwoodGiant = EnumUtils.Create<ThreatType>("DriftwoodGiant");
    public static Dictionary<string, Item> samplePrefabs = [];
    public static bool LGULoaded;
    static BepInEx.PluginInfo LGU;
    private void Awake() {
        Logger = base.Logger;
        // Lobby Compatibility stuff
        if (LobbyCompatibilityChecker.Enabled) {
            LobbyCompatibilityChecker.Init();
        }
        ModConfig = new GiantSpecimensConfig(this.Config); // Create the config with the file from here.
        GiantPatches.Init();
        Assets.PopulateAssets();

        GiantSpecimensScrap();
        GiantSpecimensEnemies();
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        // Required by https://github.com/EvaisaDev/UnityNetcodePatcher
        InitializeNetworkBehaviours();
        _harmony.PatchAll(typeof(StartOfRoundPatcher));
    }
    internal static void GiantSpecimensScrap() {
        // Whistle Item/Scrap
        Whistle = Assets.MainAssetBundle.LoadAsset<Item>("WhistleObj");
        Utilities.FixMixerGroups(Whistle.spawnPrefab);
        NetworkPrefabs.RegisterNetworkPrefab(Whistle.spawnPrefab);
        TerminalNode wlTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("wlTerminalNode");
        RegisterShopItemWithConfig(GiantSpecimensConfig.ConfigWhistleEnabled.Value, GiantSpecimensConfig.ConfigWhistleScrapEnabled.Value, Whistle, wlTerminalNode, GiantSpecimensConfig.ConfigWhistleCost.Value, GiantSpecimensConfig.ConfigWhistleRarity.Value);

        // Redwood Plushie Scrap
        RedWoodPlushie = Assets.MainAssetBundle.LoadAsset<Item>("RedWoodPlushieObj");
        Utilities.FixMixerGroups(RedWoodPlushie.spawnPrefab);
        NetworkPrefabs.RegisterNetworkPrefab(RedWoodPlushie.spawnPrefab);
        RegisterScrapWithConfig(GiantSpecimensConfig.ConfigRedwoodPlushieEnabled.Value, GiantSpecimensConfig.ConfigRedwoodPlushieRarity.Value, RedWoodPlushie);

        // Redwood Heart Scrap
        RedWoodHeart = Assets.MainAssetBundle.LoadAsset<Item>("RedwoodHeartObj");
        Utilities.FixMixerGroups(RedWoodHeart.spawnPrefab);
        NetworkPrefabs.RegisterNetworkPrefab(RedWoodHeart.spawnPrefab);

        // Driftwood Giant Sample
        DriftwoodSample = Assets.MainAssetBundle.LoadAsset<Item>("DriftWoodGiantSample");
        Utilities.FixMixerGroups(DriftwoodSample.spawnPrefab);
        NetworkPrefabs.RegisterNetworkPrefab(DriftwoodSample.spawnPrefab);
        samplePrefabs.Add("RedWoodGiant", RedWoodHeart);
        samplePrefabs.Add("DriftWoodGiant", DriftwoodSample);

    }
    internal static void GiantSpecimensEnemies() {
        // Stellar Sovereign Enemy
        /*StellarSovereign = Assets.MainAssetBundle.LoadAsset<EnemyType>("StellarSovereignObj");
        // StellarSovereign.PowerLevel = GiantSpecimensConfig.ConfigStellarSovereignPower.Value;
        TerminalNode ssTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("StellarSovereignTN");
        TerminalKeyword ssTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("StellarSovereignTK");
        NetworkPrefabs.RegisterNetworkPrefab(StellarSovereign.enemyPrefab);
        RegisterEnemyWithConfig(true, "All:9999", StellarSovereign, ssTerminalNode, ssTerminalKeyword);*/

        // Redwood Giant Enemy
        PinkGiant = Assets.MainAssetBundle.LoadAsset<EnemyType>("PinkGiantObj");
        PinkGiant.PowerLevel = GiantSpecimensConfig.ConfigRedwoodGiantPower.Value;
        TerminalNode pgTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("PinkGiantTN");
        TerminalKeyword pgTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("PinkGiantTK");
        NetworkPrefabs.RegisterNetworkPrefab(PinkGiant.enemyPrefab);
        RegisterEnemyWithConfig(GiantSpecimensConfig.ConfigRedWoodEnabled.Value, GiantSpecimensConfig.ConfigRedWoodRarity.Value, PinkGiant, pgTerminalNode, pgTerminalKeyword);
        // Driftwood Giant Enemy
        DriftGiant = Assets.MainAssetBundle.LoadAsset<EnemyType>("DriftwoodGiantObj");
        DriftGiant.PowerLevel = GiantSpecimensConfig.ConfigDriftwoodGiantPower.Value;
        TerminalNode dgTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("DriftwoodGiantTN");
        TerminalKeyword dgTerminalKeyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("DriftwoodGiantTK");
        NetworkPrefabs.RegisterNetworkPrefab(DriftGiant.enemyPrefab);
        RegisterEnemyWithConfig(GiantSpecimensConfig.ConfigDriftWoodEnabled.Value, GiantSpecimensConfig.ConfigDriftWoodRarity.Value, DriftGiant, dgTerminalNode, dgTerminalKeyword);
    }
    /*internal static void OnExtendedModRegistered(ExtendedMod extendedMod) {
        if (extendedMod == null) return;
        foreach (ExtendedEnemyType extendedEnemyType in extendedMod.ExtendedEnemyTypes) {
            List<StringWithRarity> planetNames = new List<StringWithRarity>();
            if (extendedEnemyType.name == "RedwoodExtendedEnemyType") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigRedWoodRarity.Value);
                extendedEnemyType.EnemyType.PowerLevel = GiantSpecimensConfig.ConfigRedwoodGiantPower.Value;
            } else if (extendedEnemyType.name == "DriftwoodExtendedEnemyType") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigDriftWoodRarity.Value);
                extendedEnemyType.EnemyType.PowerLevel = GiantSpecimensConfig.ConfigDriftwoodGiantPower.Value;
            }
            extendedEnemyType.OutsideLevelMatchingProperties.planetNames.AddRange(planetNames);
            Plugin.Logger.LogInfo($"Configured {extendedEnemyType.name} with new planet names and rarities.");
        }
        foreach (ExtendedItem extendedItem in extendedMod.ExtendedItems) {
            List<StringWithRarity> planetNames = new List<StringWithRarity>();
            if (extendedItem.name == "RedwoodPlushieExtendedItem") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigRedwoodPlushieRarity.Value);

            } else if (extendedItem.name == "DriftwoodPlushieExtendedItem") {
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigDriftWoodPlushieRarity.Value);

            } else if (extendedItem.name == "WhistleExtendedItem") {
                extendedItem.IsBuyableItem = GiantSpecimensConfig.ConfigWhistleEnabled.Value;
                extendedItem.Item.creditsWorth = GiantSpecimensConfig.ConfigWhistleCost.Value;
                planetNames = ConfigParsing(GiantSpecimensConfig.ConfigDriftWoodPlushieRarity.Value);

            } else if (extendedItem.name == "DriftwoodSampleExtendedItem") {
                samplePrefabs.Add("DriftWoodGiant", extendedItem.Item);
                extendedItem.LevelMatchingProperties.planetNames.AddRange(ConfigParsing("Vanilla:0,Custom:0")); // I don't think LLL does this for whatever reason

            } else if (extendedItem.name == "RedwoodHeartExtendedItem") {
                samplePrefabs.Add("RedWoodGiant", extendedItem.Item);
                extendedItem.LevelMatchingProperties.planetNames.AddRange(ConfigParsing("Vanilla:0,Custom:0")); // I don't think LLL does this for whatever reason

            }
            extendedItem.LevelMatchingProperties.planetNames.AddRange(planetNames);
            Plugin.Logger.LogInfo($"Configured {extendedItem.name} with new planet names and rarities.");
        }
        RunLGUCompatibility();
    }
    internal static void OnLethalBundleLoaded(AssetBundle assetBundle) {
            if (assetBundle == null) return;
    }*/

    internal static void RunLGUCompatibility() {
        if (Chainloader.PluginInfos.TryGetValue(Metadata.GUID, out LGU)) {
            LGULoaded = true;
            Logger.LogInfo($"MNC = {LGU}");

            if (GiantSpecimensConfig.ConfigDriftwoodHeartEnabled.Value) {
                samplePrefabs.TryGetValue("DriftWoodGiant", out Item item);
                if (item != null) {
                    RegisterLGUSample(item, "DriftWoodGiant", 2, false, true, 50);
                }
                else {
                    Logger.LogInfo($"Error registering Driftwood Giant Sample!");
                }
            }

            if (GiantSpecimensConfig.ConfigRedwoodHeartEnabled.Value) {
                samplePrefabs.TryGetValue("RedWoodGiant", out Item item);
                if (item != null) {
                    RegisterLGUSample(item, "RedWoodGiant", 3, false, true, 50);
                }
                else {
                    Logger.LogInfo($"Error registering Redwood Giant Sample!");
                }
            }
        }
    }

    /*private static List<StringWithRarity> ConfigParsing(string configMoonRarity) {
        List<StringWithRarity> spawnRates = new List<StringWithRarity>();

        foreach (string entry in configMoonRarity.Split(ConfigHelper.indexSeperator).Select(s => s.Trim())) {
            string[] entryParts = entry.Split(ConfigHelper.keyPairSeperator);
            if (entryParts.Length != 2) {
                continue;
            }

            string name = entryParts[0];
            if (int.TryParse(entryParts[1], out int spawnrate)) {
                spawnRates.Add(new StringWithRarity(name, spawnrate));
                Plugin.Logger.LogInfo($"Registered spawn rate for {name} to {spawnrate}");
            }
        }
        return spawnRates;
    }*/
    private void InitializeNetworkBehaviours() {
        IEnumerable<Type> types;
        try
        {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null);
        }
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
    private static void RegisterEnemyWithConfig(bool enabled, string configMoonRarity, EnemyType enemy, TerminalNode terminalNode, TerminalKeyword terminalKeyword) {
        if (enabled) { 
            (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
            RegisterEnemy(enemy, spawnRateByLevelType, spawnRateByCustomLevelType, terminalNode, terminalKeyword);
            return;
        } else {
            RegisterEnemy(enemy, 0, LevelTypes.All, terminalNode, terminalKeyword);
            return;
        }
    }
    private static void RegisterScrapWithConfig(bool enabled, string configMoonRarity, Item scrap) {
        if (enabled) { 
            (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
            RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
        } else {
            RegisterScrap(scrap, 0, LevelTypes.All);
        }
        return;
    }
    private static void RegisterShopItemWithConfig(bool enabledShopItem, bool enabledScrap, Item item, TerminalNode terminalNode, int itemCost, string configMoonRarity) {
        if (enabledShopItem) { 
            RegisterShopItem(item, null, null, terminalNode, itemCost);
        }
        if (enabledScrap) {
            RegisterScrapWithConfig(true, configMoonRarity, item);
        }
        return;
    }
    private static void RegisterLGUSample(Item sample, string monster, int level, bool registerNetworkPrefab, bool grabbableToEnemies, double weight = 50) {
        if (LGULoaded) {
            var type = typeof(MoreShipUpgrades.API.HunterSamples);
            MethodInfo info = type.GetMethod("RegisterSample", [typeof(Item), typeof(string), typeof(int), typeof(bool), typeof(bool), typeof(double)]);
            info!.Invoke(null, [sample, monster, level, registerNetworkPrefab, grabbableToEnemies, weight]);
        }
    }
    private static (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ConfigParsing(string configMoonRarity) {
        Dictionary<LevelTypes, int> spawnRateByLevelType = new Dictionary<LevelTypes, int>();
        Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();
        foreach (string entry in configMoonRarity.Split(',').Select(s => s.Trim())) {
            string[] entryParts = entry.Split(':');

            if (entryParts.Length != 2) {
                continue;
            }
            string name = entryParts[0];
            int spawnrate;

            if (!int.TryParse(entryParts[1], out spawnrate)) {
                continue;
            }

            if (Enum.TryParse<LevelTypes>(name, true, out LevelTypes levelType)) {
                spawnRateByLevelType[levelType] = spawnrate;
                Plugin.Logger.LogInfo($"Registered spawn rate for level type {levelType} to {spawnrate}");
            } else {
                spawnRateByCustomLevelType[name] = spawnrate;
                Plugin.Logger.LogInfo($"Registered spawn rate for custom level type {name} to {spawnrate}");
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }
}
public static class Assets {
    public static AssetBundle MainAssetBundle = null;
    public static void PopulateAssets() {
        string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "giantspecimensassetsll"));
        if (MainAssetBundle == null) {
            Plugin.Logger.LogError("Failed to load custom assets.");
            return;
        }
    }
}