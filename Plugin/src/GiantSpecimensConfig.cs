
using BepInEx;
using BepInEx.Configuration;

namespace GiantSpecimens {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class GiantSpecimensConfig : BaseUnityPlugin
    {
        public ConfigEntry<int> configSpawnrateForest;
        public ConfigEntry<int> configSpawnrateRedWood;

        void Awake()
        {
            configSpawnrateForest = Config.Bind("Spawnrates",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                1, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file

            configSpawnrateRedWood = Config.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            Logger.LogInfo("Setting up config for Giant Specimen plugin...");
        }
    }
}