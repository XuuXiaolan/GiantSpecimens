
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
        public ConfigEntry<string> configSpawnRateEntries { get; private set; }


        // Here we make a new object, passing in the config file from Plugin.cs
        public GiantSpecimensConfig(ConfigFile configFile) 
        {
            configSpawnrateForest = configFile.Bind("Vanilla Spawnrates",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                4, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file

            configSpawnRateEntries = configFile.Bind("Vanilla Spawnrates", 
                                                "RedWood Giant Spawn Weight.",
                                                "ExperimentationLevel@50,AssuranceLevel@100,VowLevel@200,OffenseLevel@100,MarchLevel@200,RendLevel@200,DineLevel@100,TitanLevel@200,Modded@100",
                                                "Spawn Weight of the RedWood Giant in all vanilla moons + a universal modded option (doesn't work for LLL moons yet), just replace the number below with a custom spawnrate if you're changing it, do not change the format");

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