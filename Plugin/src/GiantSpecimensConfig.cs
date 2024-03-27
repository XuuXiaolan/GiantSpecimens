
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace GiantSpecimens {
    public class GiantSpecimensConfig
    {
        public ConfigEntry<int> ConfigSpawnrateForest { get; private set; }
        public ConfigEntry<string> ConfigSpawnRateEntries { get; private set; }
        public ConfigEntry<float> ConfigSpeedRedWood { get; private set; }
        public ConfigEntry<float> ConfigShipDistanceRedWood { get; private set; }
        public ConfigEntry<float> ConfigForestDistanceRedWood { get; private set; }
        public ConfigEntry<string> ConfigColourHexcode { get; private set; }
        public ConfigEntry<string> ConfigScrapRarity { get; private set; }
        public ConfigEntry<bool> ConfigScrapEnabled { get; private set; }
        public ConfigEntry<int> ConfigWhistleCost { get; private set; }
        public ConfigEntry<bool> ConfigWhistleEnabled { get; private set; }
        public ConfigEntry<string> ConfigWhistleRarity { get; private set; }
        public ConfigEntry<bool> ConfigWhistleScrapEnabled { get; private set; }


        // Here we make a new object, passing in the config file from Plugin.cs
        public GiantSpecimensConfig(ConfigFile configFile) 
        {
            ConfigSpawnrateForest = configFile.Bind("Spawnrates",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                4, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file
            ConfigSpawnRateEntries = configFile.Bind("Spawnrates", 
                                                "RedWood Giant | Spawn Weight.",
                                                "Modded@100,ExperimentationLevel@50,AssuranceLevel@100,VowLevel@200,OffenseLevel@100,MarchLevel@200,RendLevel@200,DineLevel@100,TitanLevel@200,46 Infernis@100,76 Porcerin@200,154 Etern@150,57 Asteroid13@200,147 Gratar@100,94 Polarus@150,44 Atlantica@25,42 Cosmocos@200,84 Junic@150,36 Gloom@200,48 Desolation@150,134 Oldred@100",
                                                "Spawn Weight of the RedWood Giant in all vanilla moons + Wesley's moons modded option (Doesn't work for LLL moons yet), just replace the number below with a custom spawnrate if you're changing it, do not change the format.");
            ConfigSpeedRedWood = configFile.Bind("Misc Options",   
                                                "RedWood Giant Speed",  
                                                2f, 
                                                "Default walking speed of the RedWood Giant, (Chase speed is 4*Walking Speed) I recommend 1.5 to 3."); 
            ConfigShipDistanceRedWood = configFile.Bind("Misc Options",   
                                                "RedWood Giant Targetting Range | Ship",  
                                                10f, 
                                                "Distance of the Forest Keeper to the ship that stops the RedWoodGiant from chasing them, I recommend 0 to 15f (values are completely untested)."); 
            ConfigForestDistanceRedWood = configFile.Bind("Misc Options",   
                                                "RedWood Giant Targetting Range | Forest Keeper",  
                                                50f, 
                                                "Distance from which the RedWood Giant is able to see the Forest Keeper, I recommend 30f or more.");
            ConfigColourHexcode = configFile.Bind("Misc Options",   
                                                "RedWood Giant | Footstep Colour",  
                                                "#808080", 
                                                "Decides what the default colour of the footsteps is using a hexcode, default is grey (Invalid hexcodes will default to Grey), keep blank to use custom set colours set by me for different moons, don't forget to include the hashtag in config."); 
            ConfigWhistleScrapEnabled = configFile.Bind("Scrap Options",
                                                "Whistle Scrap | Enabled",
                                                true,
                                                "Enables/Disables the spawning of the scrap");
            ConfigWhistleRarity = configFile.Bind("Scrap Options",   
                                                "Whistle Item | Rarity",  
                                                "Modded@5,ExperimentationLevel@5,AssuranceLevel@5,VowLevel@5,OffenseLevel@5,MarchLevel@5,RendLevel@5,DineLevel@5,TitanLevel@5", 
                                                "Rarity of Whistle scrap appearing on every moon");
            ConfigScrapEnabled = configFile.Bind("Scrap Options",
                                                "RedWood Giant Scrap | Enabled",
                                                true,
                                                "Enables/Disables the spawning of the scrap");
            ConfigScrapRarity = configFile.Bind("Scrap Options",   
                                                "RedWood Giant Scrap | Rarity",  
                                                "Modded@5,ExperimentationLevel@5,AssuranceLevel@5,VowLevel@5,OffenseLevel@5,MarchLevel@5,RendLevel@5,DineLevel@5,TitanLevel@5", 
                                                "Rarity of scrap appearing on every moon");
            ConfigWhistleEnabled = configFile.Bind("Shop Options",   
                                                "Whistle Item | Enabled",  
                                                true, 
                                                "Enables/Disables the whistle showing up in shop");
            ConfigWhistleCost = configFile.Bind("Shop Options",   
                                                "Whistle Item | Cost",  
                                                100, 
                                                "Cost of Whistle");
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