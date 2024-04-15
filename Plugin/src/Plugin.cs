using BepInEx;
using BepInEx.Logging;
using GiantSpecimens.Dependency;
using GiantSpecimens.Configs;
using GiantSpecimens.Patches;

namespace GiantSpecimens {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin {
        public static GiantSpecimensConfig ModConfig { get; private set; } // prevent from accidently overriding the config
        internal static new ManualLogSource Logger;
        public static CauseOfDeath RupturedEardrums = EnumUtils.Create<CauseOfDeath>("RupturedEardrums");
        public static CauseOfDeath InternalBleed = EnumUtils.Create<CauseOfDeath>("InternalBleed");
        public static CauseOfDeath Thwomped = EnumUtils.Create<CauseOfDeath>("Thwomped");
        public static ThreatType DriftwoodGiant = EnumUtils.Create<ThreatType>("DriftwoodGiant");
        private void Awake() {
            Logger = base.Logger;
            // Lobby Compatibility stuff
            if (LobbyCompatibilityChecker.Enabled) {
                LobbyCompatibilityChecker.Init();
            }

            ModConfig = new GiantSpecimensConfig(this.Config); // Create the config with the file from here.
            GiantPatches.Init();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}