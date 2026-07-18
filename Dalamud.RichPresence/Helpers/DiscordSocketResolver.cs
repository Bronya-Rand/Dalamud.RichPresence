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
        }
        public static string? FindSocket(int pipe)
        {
            // Exit if not on Wine
            if (!Util.IsWine())
                return null;

            Plugin.Log.Debug($"Searching for Discord socket (pipe {pipe}) on Wine...");
            var xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (xdgRuntimeDir.IsNullOrEmpty())
            {
                Plugin.Log.Warning("XDG_RUNTIME_DIR environment variable is not set. Cannot find Discord socket.");
                return null;
            }

            // Look for the Discord socket (0-9)
            foreach (var subdir in SandboxSubdirectories)
            {
                string basePath = Path.Combine(xdgRuntimeDir, subdir);
                string socketPath = Path.Combine(basePath, $"discord-ipc-{pipe}");

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
