
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace GiantSpecimens {
    public class GiantSpecimensConfig
    {
        public ConfigEntry<int> configSpawnrateForest { get; private set; }
        public ConfigEntry<string> configSpawnRateEntries { get; private set; }
        public ConfigEntry<float> configSpeedRedWood { get; private set; }
        public ConfigEntry<float> configShipDistanceRedWood { get; private set; }
        public ConfigEntry<float> configForestDistanceRedWood { get; private set; }

        // Here we make a new object, passing in the config file from Plugin.cs
        public GiantSpecimensConfig(ConfigFile configFile) 
        {
            configSpawnrateForest = configFile.Bind("ForestKeeper Spawnrates",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                4, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file

            configSpawnRateEntries = configFile.Bind("Moon Spawnrates", 
                                                "RedWood Giant Spawn Weight.",
                                                "ExperimentationLevel@50,AssuranceLevel@100,VowLevel@200,OffenseLevel@100,MarchLevel@200,RendLevel@200,DineLevel@100,TitanLevel@200,Modded@100,InfernisLevel@100,PorcerinLevel@200,EternLevel@150,Asteroid13Level@200,GratarLevel@100,PolarusLevel@150,AtlanticaLevel@25,CosmocosLevel@200,JunicLevel@150,GloomLevel@200,DesolationLevel@150,OldredLevel@100",
                                                "Spawn Weight of the RedWood Giant in all vanilla moons + Wesley's moons modded option (Adding Generic's moons next, also doesn't work for LLL moons yet), just replace the number below with a custom spawnrate if you're changing it, do not change the format.");

            configSpeedRedWood = configFile.Bind("Misc Options",   
                                                "RedWood Giant Speed",  
                                                2f, 
                                                "Default walking speed of the RedWood Giant, (Chase speed is 4*Walking Speed) I recommend 1.5 to 3."); 

            configShipDistanceRedWood = configFile.Bind("Misc Options",   
                                                "RedWood Forest Keeper Targetting Range | Ship",  
                                                10f, 
                                                "Distance of the Forest Keeper to the ship that stops the RedWoodGiant from chasing them, I recommend 0 to 15f (values are completely untested)."); 
            configForestDistanceRedWood = configFile.Bind("Misc Options",   
                                                "RedWood Forest Keeper Targetting Range | Forest Keeper",  
                                                50f, 
                                                "Distance from which the RedWood Giant is able to see the Forest Keeper, I recommend 30f or more."); 
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