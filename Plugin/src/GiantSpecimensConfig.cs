
using BepInEx;
using BepInEx.Configuration;

namespace GiantSpecimens {
    public class GiantSpecimensConfig
    {
        public ConfigEntry<int> configSpawnrateForest;
        public ConfigEntry<int> configSpawnrateRedWood;

        // Here we make a new object, passing in the config file from Plugin.cs
        public GiantSpecimensConfig(ConfigFile configFile) 
        {
            configSpawnrateForest = configFile.Bind("Spawnrates",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                1, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file

            configSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            Plugin.Logger.LogInfo("Setting up config for Giant Specimen plugin...");
        }
    }
}