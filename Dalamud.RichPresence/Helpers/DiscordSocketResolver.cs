using System;
using System.IO;
using System.Net.Sockets;
using Dalamud.Utility;

namespace Dalamud.RichPresence.Helpers
{
    internal static class DiscordSocketResolver
    {
        private static readonly string[] SandboxSubdirectories =
        [
            "", // Native
            // Discord Flatpaks
            "app/com.discordapp.Discord",
            "app/com.discordapp.DiscordPTB",
            "app/com.discordapp.DiscordCanary",
            // Vesktop Flatpak
            "app/dev.vencord.Vesktop",
            // Snap
            "snap.discord"
        ];

        // Other possible locations for Discord's socket
        private static readonly string[] TempDirEnvVars = ["TMPDIR", "TEMP", "TMP"];

        private static bool? AFUnixSupported;

        public static bool IsAfUnixSupported()
        {
            if (AFUnixSupported.HasValue) return AFUnixSupported.Value;

            try
            {
                using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                AFUnixSupported = true;
            }
            catch (SocketException)
            {
                AFUnixSupported = false;
            }

            return AFUnixSupported.Value;
        }
        private static bool TrySocketExists(string socketPath)
        {
            using var testSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                testSocket.Connect(new UnixDomainSocketEndPoint(socketPath));
                try { testSocket.Shutdown(SocketShutdown.Both); } catch { }
                return true;

            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Unexpected error while checking socket existence: {ex.Message}");
                return false;
            }
        }
        private static string? ResolveRuntimeBaseDir()
        {
            var xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (!xdgRuntimeDir.IsNullOrEmpty())
                return xdgRuntimeDir;

            var wineHostXDGRuntimeDir = Environment.GetEnvironmentVariable("WINE_HOST_XDG_RUNTIME_DIR");
            if (!wineHostXDGRuntimeDir.IsNullOrEmpty())
                return wineHostXDGRuntimeDir;

            // Test fallback environment variables for temp directories
            foreach (var envVar in TempDirEnvVars)
            {
                var tempDir = Environment.GetEnvironmentVariable(envVar);
                if (!tempDir.IsNullOrEmpty())
                    return tempDir;
            }

            Plugin.Log.Warning("XDG_RUNTIME_DIR and temp directory environment variables are not set. Possible Wine Bug or missing environment variable.");
            return null;
        }
        public static string? FindSocket(int pipe)
        {
            // Exit if not on Wine
            if (!Util.IsWine() || !IsAfUnixSupported())
                return null;

            Plugin.Log.Info($"Searching for Discord socket (pipe {pipe}) on Wine...");
            var runtimeDir = ResolveRuntimeBaseDir();
            Plugin.Log.Debug($"Resolved runtime directory: {runtimeDir ?? "null"}");
            if (runtimeDir.IsNullOrEmpty())
            {
                Plugin.Log.Warning("Could not resolve a valid runtime directory for Discord socket search. Please check your environment variable configuration.");
                return null;
            }

            // Look for the Discord socket (0-9)
            foreach (var subdir in SandboxSubdirectories)
            {
                string basePath = Path.Combine(runtimeDir, subdir);
                string socketPath = Path.Combine(basePath, $"discord-ipc-{pipe}").Replace("\\", "/");

                Plugin.Log.Debug($"Trying Discord socket at: {socketPath}");
                if (TrySocketExists(socketPath))
                {
                    Plugin.Log.Info($"Discord socket (pipe {pipe}) found at: {socketPath}");
                    return socketPath;
                }
            }

            Plugin.Log.Debug($"Discord socket (pipe {pipe}) not found.");
            return null;
        }
    }
}
