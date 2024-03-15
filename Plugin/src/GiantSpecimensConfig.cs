
using System.Collections.Generic;
using System.Reflection;
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
                                                "Spawn Weight of the RedWood Giant in Experimentation");
            configAssuranceSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                100,
                                                "Spawn Weight of the RedWood Giant in Assurance");
            configVowSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant in Vow");
            configOffenseSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                100,
                                                "Spawn Weight of the RedWood Giant in Offense");
            configMarchSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant in March");
            configRendSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                150,
                                                "Spawn Weight of the RedWood Giant in Rend");
            configDineSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                150,
                                                "Spawn Weight of the RedWood Giant in Dine");
            configTitanSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant in Titan");
            configModdedSpawnrateRedWood = configFile.Bind("Spawnrates", 
                                                "RedWood Giant Spawn Weight",
                                                200,
                                                "Spawn Weight of the RedWood Giant in all modded moons.");
            ClearUnusedEntries(configFile);
            Plugin.Logger.LogInfo("Setting up config for Giant Specimen plugin...");
        }

        private void ClearUnusedEntries(ConfigFile configFile) {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            configFile.Save(); // Save the config file to save these changes
        }
    }
}