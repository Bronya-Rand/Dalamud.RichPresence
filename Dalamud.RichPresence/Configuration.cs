using Dalamud.Configuration;
using Newtonsoft.Json;

namespace Dalamud.RichPresence
{
    class Configuration : IPluginConfiguration
    {
        private const int CurrentVersion = 2;

        public int Version { get; set; } = CurrentVersion;

        // Show login queue position
        public bool ShowLoginQueuePosition = true;
        // Show character name
        public bool ShowName = true;
        // Show Free Company Tag
        public bool ShowFreeCompany = true;
        // Show world name
        public bool ShowWorld = true;
        // Always show home world in details (even when on home world)
        public bool AlwaysShowHomeWorld = false;
        // show data center name alongside world
        public bool ShowDataCenter = false;

        // Show elapsed time in zones
        public bool ShowStartTime = false;
        // Reset timer when changing zones
        public bool ResetTimeWhenChangingZones = true;

        // Show current job
        public bool ShowJob = true;
        // Abbreviate current job name
        public bool AbbreviateJob = true;
        // Show current job level
        public bool ShowLevel = true;

        public bool ShowParty = true;

        public bool ShowAfk = true;
        public bool HideEntirelyWhenAfk = false;
        public bool HideInCutscene = false;

        // On Linux/macOS (under Wine), connect directly to Discord's IPC socket instead of
        // relying on an external bridge to provide the named pipe. Renamed from the former
        // "RPCBridgeEnabled" option in configuration v2 (see Migrate()).
        public bool ConnectDirectlyOnWine = true;

        // Legacy alias for ConnectDirectlyOnWine, retained only to migrate pre-v2
        // configurations. It is cleared (and dropped from disk) once Migrate() runs.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? RPCBridgeEnabled = null;

        /// <summary>
        /// Migrates older saved configurations in place. Returns true if anything changed
        /// and the configuration should be persisted.
        /// </summary>
        public bool Migrate()
        {
            var changed = false;

            // v1 -> v2: "RPCBridgeEnabled" was renamed to "ConnectDirectlyOnWine".
            if (RPCBridgeEnabled.HasValue)
            {
                ConnectDirectlyOnWine = RPCBridgeEnabled.Value;
                RPCBridgeEnabled = null;
                changed = true;
            }

            if (Version < CurrentVersion)
            {
                Version = CurrentVersion;
                changed = true;
            }

            return changed;
        }
    }
}
