using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GiantSpecimens {

    public enum LevelTag
    {
        Desolate,
        Wasteland,
        Forest,
        Snow,
        Ice,
        Tundra,
        Mesa,
        Jungle,
        Desert,
        Arctic,
        Savannah,
        Swamp,
        Volcanic,
        Urban,
        Ruins,
        Underwater,
        Cave,
        Mountain,
        Plains,
        Sky,
        Space,
        Lava,
        Magical,
        Cybernetic,
        Industrial,
        Coastal,
        Rainforest,
        Dark,
        Arid,
    }

    public class LevelColorMapper
    {
        private readonly Dictionary<string, List<LevelTag>> LevelNamesAndTheirTags = new Dictionary<string, List<LevelTag>>
        {
            { "ExperimentationLevel", new List<LevelTag> { LevelTag.Mesa, LevelTag.Desolate } },
            { "AssuranceLevel", new List<LevelTag> { LevelTag.Desert } },
            { "VowLevel", new List<LevelTag> { LevelTag.Forest, LevelTag.Jungle } },
            { "OffenseLevel", new List<LevelTag> { LevelTag.Volcanic, LevelTag.Desolate } },
            { "MarchLevel", new List<LevelTag> { LevelTag.Plains, LevelTag.Savannah } },
            { "RendLevel", new List<LevelTag> { LevelTag.Ruins, LevelTag.Urban } },
            { "DineLevel", new List<LevelTag> { LevelTag.Forest, LevelTag.Mountain } },
            { "TitanLevel", new List<LevelTag> { LevelTag.Mountain, LevelTag.Snow } },
            { "InfernisLevel", new List<LevelTag> { LevelTag.Lava, LevelTag.Volcanic } },
            { "PorcerinLevel", new List<LevelTag> { LevelTag.Swamp, LevelTag.Rainforest } },
            { "EternLevel", new List<LevelTag> { LevelTag.Magical, LevelTag.Forest } },
            { "Asteroid13Level", new List<LevelTag> { LevelTag.Dark, LevelTag.Desolate } },
            { "GratarLevel", new List<LevelTag> { LevelTag.Desert, LevelTag.Arid } },
            { "PolarusLevel", new List<LevelTag> { LevelTag.Snow, LevelTag.Ice, LevelTag.Arctic } },
            { "AtlanticaLevel", new List<LevelTag> { LevelTag.Underwater, LevelTag.Coastal } },
            { "CosmocosLevel", new List<LevelTag> { LevelTag.Space, LevelTag.Cybernetic } },
            { "JunicLevel", new List<LevelTag> { LevelTag.Plains, LevelTag.Savannah } },
            { "GloomLevel", new List<LevelTag> { LevelTag.Swamp, LevelTag.Dark } }, // Assuming 'Dark' is a tag for gloomy, poorly lit environments
            { "DesolationLevel", new List<LevelTag> { LevelTag.Desolate, LevelTag.Wasteland } },
            { "OldredLevel", new List<LevelTag> { LevelTag.Ruins, LevelTag.Desolate } },
            { "AuralisLevel", new List<LevelTag> { LevelTag.Ice, LevelTag.Cybernetic } },
        };

        private readonly Dictionary<LevelTag, string> TagToColor = new Dictionary<LevelTag, string>
        {
            // Add or update entries with hex codes
            { LevelTag.Desolate, "#FF0000" }, // Red
            { LevelTag.Wasteland, "#808080" }, // Grey
            { LevelTag.Forest, "#228B22" }, // Forest Green
            { LevelTag.Snow, "#FFFAFA" }, // Snow
            { LevelTag.Ice, "#ADD8E6" }, // Light Blue (Ice)
            { LevelTag.Tundra, "#C0D6E4" }, // Lighter Greyish Blue (Tundra)
            { LevelTag.Mesa, "#CC7722" }, // Ochre (Mesa)
            { LevelTag.Jungle, "#00A550" }, // Jungle Green
            { LevelTag.Desert, "#EDC9Af" }, // Desert Sand
            { LevelTag.Arctic, "#BED8D4" }, // Arctic Blue
            { LevelTag.Savannah, "#E9DDC7" }, // Savannah Yellow
            { LevelTag.Swamp, "#697D89" }, // Swamp Grey
            { LevelTag.Volcanic, "#FF4500" }, // OrangeRed (Volcanic)
            { LevelTag.Urban, "#787878" }, // Concrete Grey
            { LevelTag.Ruins, "#A9A9A9" }, // Dusty Grey
            { LevelTag.Underwater, "#1E90FF" }, // Dodger Blue
            { LevelTag.Cave, "#4B0082" }, // Indigo
            { LevelTag.Mountain, "#8B4513" }, // Saddle Brown
            { LevelTag.Plains, "#7CFC00" }, // Lawn Green
            { LevelTag.Sky, "#87CEEB" }, // Sky Blue
            { LevelTag.Space, "#000080" }, // Navy
            { LevelTag.Lava, "#CF1020" }, // Lava Red
            { LevelTag.Magical, "#DA70D6" }, // Orchid
            { LevelTag.Cybernetic, "#00FFFF" }, // Aqua
            { LevelTag.Industrial, "#708090" }, // Slate Grey
            { LevelTag.Coastal, "#2E8B57" }, // Sea Green
            { LevelTag.Rainforest, "#006400" }, // Dark Green
            { LevelTag.Arid, "#EDEAC2" }, // Light Yellow
            { LevelTag.Dark, "#0A0F0D" }, // Very dark shade
        };

        public List<string> GetColorsForLevel(string levelName)
        {
            if (!LevelNamesAndTheirTags.ContainsKey(levelName))
            {
                throw new WarningException("Level name not found.", nameof(levelName));
            }

            var colors = new List<string>();
            foreach (var tag in LevelNamesAndTheirTags[levelName])
            {
                if (TagToColor.TryGetValue(tag, out var color))
                {
                    colors.Add(color);
                }
            }
            return colors;
        }
    }
}