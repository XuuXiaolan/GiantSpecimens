
using BepInEx;
using BepInEx.Configuration;

namespace GiantSpecimens {
    public class GiantSpecimensConfig
    {
        public ConfigEntry<int> configSpawnrateForest { get; private set; }
        public ConfigEntry<int> configExperimentationSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configAssuranceSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configVowSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configOffenseSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configMarchSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configRendSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configDineSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configTitanSpawnrateRedWood { get; private set; }
        public ConfigEntry<int> configModdedSpawnrateRedWood { get; private set; }


        // Here we make a new object, passing in the config file from Plugin.cs
        public GiantSpecimensConfig(ConfigFile configFile) 
        {
            configSpawnrateForest = configFile.Bind("Spawnrates",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                4, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file

            configExperimentationSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                50,
                                                "Spawn Weight of the RedWood Giant");
            configAssuranceSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configVowSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configOffenseSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configMarchSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configRendSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configDineSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configTitanSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            configModdedSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant");
            Plugin.Logger.LogInfo("Setting up config for Giant Specimen plugin...");
        }
    }
}