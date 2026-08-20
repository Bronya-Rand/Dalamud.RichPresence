using System;
using System.IO;
using System.Net.Sockets;

namespace Dalamud.RichPresence.Helpers
{
    /// <summary>
    /// A shared class between the Discord RPC Unix socket and the Discord
    /// RPC TCP bridge that resolves the Discord socket path on Wine, Linux and macOS
    /// </summary>
    public class SocketResolver
    {
        public Action<string>? LogCallback;
        public Action<string>? LogDebugCallback;
        public Action<Exception?, string>? LogErrorCallback;
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
        private static readonly string[] TempDirEnvVars = ["TMPDIR", "TMP", "TEMP"];
        private bool TrySocketExists(string socketPath)
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
                LogErrorCallback?.Invoke(ex, $"Unexpected error while checking socket existence at {socketPath}");
                return false;
            }
        }

        /// <summary>
        /// Resolves the base directory for where the Discord socket would be located.
        /// </summary>
        /// <returns>The base directory for the runtime.</returns>
        public static string ResolveRuntimeBaseDir()
        {
            // Check both instances of XDG_RUNTIME_DIR
            var xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (!string.IsNullOrEmpty(xdgRuntimeDir))
                return xdgRuntimeDir;

            var wineHostXDGRuntimeDir = Environment.GetEnvironmentVariable("WINE_HOST_XDG_RUNTIME_DIR");
            if (!string.IsNullOrEmpty(wineHostXDGRuntimeDir))
                return wineHostXDGRuntimeDir;

            // Test fallback environment variables for temp directories
            foreach (var envVar in TempDirEnvVars)
            {
                var tempDir = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(tempDir))
                    return tempDir;
            }

            // According to Discord docs, `/tmp` is the final fallback to check for the socket
            return "/tmp";
        }

        /// <summary>
        /// Finds the Discord socket path for the given pipe number (0-9) on
        /// Wine, Linux, and macOS.
        /// </summary>
        /// <param name="pipe">The pipe number (0-9) to search for.</param>
        /// <returns>The path to the Discord socket, or null if not found.</returns>
        public string? FindSocket(int pipe)
        {
            LogCallback?.Invoke($"Searching for Discord socket (pipe {pipe})...");
            var runtimeDir = ResolveRuntimeBaseDir();
            LogDebugCallback?.Invoke($"Resolved runtime directory: {runtimeDir}");

            // Look for the Discord socket (0-9)
            foreach (var subdir in SandboxSubdirectories)
            {
                string basePath = Path.Combine(runtimeDir, subdir);
                string socketPath = Path.Combine(basePath, $"discord-ipc-{pipe}").Replace("\\", "/");

                LogDebugCallback?.Invoke($"Trying Discord socket at: {socketPath}");
                if (TrySocketExists(socketPath))
                {
                    LogCallback?.Invoke($"Discord socket (pipe {pipe}) found at: {socketPath}");
                    return socketPath;
                }
            }

            LogDebugCallback?.Invoke($"Discord socket (pipe {pipe}) not found.");
            return null;
        }
    }
}
