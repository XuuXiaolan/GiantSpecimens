
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace GiantSpecimens.Configs {
    public class GiantSpecimensConfig {
        public ConfigEntry<int> ConfigMultiplierForestkeeper { get; private set; }
        public ConfigEntry<float> ConfigSpeedRedWood { get; private set; }
        public ConfigEntry<float> ConfigShipDistanceRedWood { get; private set; }
        public ConfigEntry<float> ConfigForestDistanceRedWood { get; private set; }
        public ConfigEntry<string> ConfigColourHexcode { get; private set; }
        public ConfigEntry<bool> ConfigRedWoodEnabled { get; private set; }
        public ConfigEntry<string> ConfigRedWoodRarity { get; private set; }
        public ConfigEntry<bool> ConfigRedwoodPlushieEnabled { get; private set; }
        public ConfigEntry<string> ConfigRedwoodPlushieRarity { get; private set; }
        public ConfigEntry<int> ConfigWhistleCost { get; private set; }
        public ConfigEntry<bool> ConfigWhistleEnabled { get; private set; }
        public ConfigEntry<string> ConfigWhistleRarity { get; private set; }
        public ConfigEntry<bool> ConfigWhistleScrapEnabled { get; private set; }
        public ConfigEntry<bool> ConfigDriftWoodEnabled { get; private set; }
        public ConfigEntry<string> ConfigDriftWoodRarity { get; private set; }
        public ConfigEntry<bool> ConfigDriftWoodPlushieEnabled { get; private set; }
        public ConfigEntry<string> ConfigDriftWoodPlushieRarity { get; private set; }
        public ConfigEntry<int> ConfigMultiplierDriftwood { get; private set; }
        public GiantSpecimensConfig(ConfigFile configFile) {
            ConfigMultiplierForestkeeper = configFile.Bind("Enemy Options",   // The section under which the option is shown
                                                "ForestKeeper Multiplier",  // The key of the configuration option in the configuration file
                                                2, // The default value
                                                "Multiplier in Forest Keeper spawnrate after the RedWood Giant spawns."); // Description of the option to show in the config file
            ConfigMultiplierDriftwood = configFile.Bind("Enemy Options",
                                                        "Driftwood Multiplier",
                                                        2,
                                                        "Multiplier in Driftwood spawnrate after the RedWood Giant spawns");
            ConfigRedWoodEnabled = configFile.Bind("Enemy Options",
                                                "RedWood Giant | Enabled",
                                                true,
                                                "Enables/Disables the spawning of the RedWood Giant (sets rarity to 0 if false on all moons)"); // Description of the option to show in the config file
            ConfigRedWoodRarity = configFile.Bind("Enemy Options", 
                                                "RedWood Giant | Spawn Weight.",
                                                "Modded@100,Experimentation@50,Assurance@100,Vow@200,Offense@100,March@200,Rend@200,Dine@100,Titan@200",
                                                "Spawn Weight of the RedWood Giant in all moons, Feel free to add to it any moon, just follow the format (also needs LLL installed for LE moons to work with this config).");
            ConfigSpeedRedWood = configFile.Bind("Enemy Options",   
                                                "RedWood Giant Speed",  
                                                2f, 
                                                "Default walking speed of the RedWood Giant, (Chase speed is 4*Walking Speed) I recommend 1.5 to 3."); 
            ConfigShipDistanceRedWood = configFile.Bind("Enemy Options",   
                                                "RedWood Giant Targetting Range | Ship",  
                                                10f, 
                                                "Distance of the Forest Keeper to the ship that stops the RedWoodGiant from chasing them, I recommend 0 to 15f (values are completely untested)."); 
            ConfigForestDistanceRedWood = configFile.Bind("Enemy Options",   
                                                "RedWood Giant Targetting Range | Forest Keeper",  
                                                50f, 
                                                "Distance from which the RedWood Giant is able to see the Forest Keeper, I recommend 30f or more.");
            ConfigColourHexcode = configFile.Bind("Enemy Options",   
                                                "RedWood Giant | Footstep Colour",  
                                                "#808080", 
                                                "Decides what the default colour of the footsteps is using a hexcode, default is grey (Invalid hexcodes will default to Grey), keep blank to use custom set colours set by me for different moons, don't forget to include the hashtag in config."); 
            ConfigWhistleScrapEnabled = configFile.Bind("Scrap Options",
                                                "Whistle Scrap | Enabled",
                                                true,
                                                "Enables/Disables the spawning of the scrap (sets rarity to 0 if false on all moons)");
            ConfigWhistleRarity = configFile.Bind("Scrap Options",   
                                                "Whistle Scrap | Rarity",  
                                                "Modded@5,Experimentation@5,Assurance@5,Vow@5,Offense@5,March@5,Rend@5,Dine@5,Titan@5", 
                                                "Rarity of Whistle scrap appearing on every moon");
            ConfigRedwoodPlushieEnabled = configFile.Bind("Scrap Options",
                                                "RedWood Giant Scrap | Enabled",
                                                true,
                                                "Enables/Disables the spawning of the scrap (sets rarity to 0 if false on all moons)");
            ConfigRedwoodPlushieRarity = configFile.Bind("Scrap Options",   
                                                "RedWood Giant Scrap | Rarity",  
                                                "Modded@5,Experimentation@5,Assurance@5,Vow@5,Offense@5,March@5,Rend@5,Dine@5,Titan@5", 
                                                "Rarity of redwood plushie appearing on every moon");
            ConfigWhistleEnabled = configFile.Bind("Shop Options",   
                                                "Whistle Item | Enabled",  
                                                true, 
                                                "Enables/Disables the whistle showing up in shop");
            ConfigWhistleCost = configFile.Bind("Shop Options",   
                                                "Whistle Item | Cost",  
                                                100, 
                                                "Cost of Whistle");
            ConfigDriftWoodEnabled = configFile.Bind("Enemy Options",
                                                    "Driftwood | Enabled",
                                                    true,
                                                    "Enables/Disables the spawning of the driftwood (sets rarity to 0 if false on all moons)");
            ConfigDriftWoodPlushieEnabled = configFile.Bind("Scrap Options",
                                                            "Driftwood Scrap | Enabled",
                                                            true,
                                                            "Enables/Disables the spawning of the scrap (sets rarity to 0 if false on all moons)");
            ConfigDriftWoodRarity = configFile.Bind("Enemy Options",
                                                    "Driftwood | Rarity",
                                                    "Modded@100,Experimentation@75,Assurance@50,Vow@150,Offense@50,March@175,Rend@125,Dine@125,Titan@150",
                                                    "Rarity of driftwood appearing on every moon");
            ConfigDriftWoodPlushieRarity = configFile.Bind("Scrap Options",
                                                        "Driftwood Scrap | Rarity",
                                                        "Modded@5,Experimentation5,Assurance5,Vow@5,Offense5,March@5,Rend@5,Dine@5,Titan@5",
                                                        "Rarity of driftwood plushie appearing on every moon.");
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