using System;
using System.Diagnostics;
using System.IO;
using Dalamud.Utility;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Services
{
    internal class DiscordService : IDisposable
    {
        private static DirectoryInfo WineRpcBridgePath => new(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, "Resources/binaries", "WineRPCBridge.exe"));

        private const string DiscordClientId = "478143453536976896";
        private DiscordRpcClient rpcClient = null!;
        private Process? rpcBridgeProcess;

        public DiscordService(Plugin plugin)
        {
            var configuration1 = plugin.Configuration;

            CreateClient();

            if (Util.IsWine() && configuration1.RpcBridgeEnabled)
            {
                StartWineRpcBridge();
            }
        }
        private void CreateClient()
        {
            if (rpcClient == null || rpcClient.IsDisposed)
            {
                rpcClient = new DiscordRpcClient(DiscordClientId)
                {
                    SkipIdenticalPresence = true,

                    Logger = new ConsoleLogger { Level = LogLevel.Warning }
                };
                rpcClient.OnPresenceUpdate += (sender, e) => { Plugin.Log.Debug($"Received Presence Update: {e.Presence}"); };
            }

            if (!rpcClient.IsInitialized)
                rpcClient.Initialize();
        }

        private void StartWineRpcBridge()
        {
            try
            {
                // Find existing bridge process.
                var wineBridge = Process.GetProcessesByName(WineRpcBridgePath.Name);
                if (wineBridge.Length > 0)
                {
                    Plugin.Log.Info($"Wine RPC Bridge found (PID: {wineBridge[0].Id}), skipping creation.");
                    rpcBridgeProcess = wineBridge[0];
                    return;
                }

                // Start the bridge.
                Plugin.Log.Info($"Starting Wine RPC Bridge: {WineRpcBridgePath}");
                rpcBridgeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = WineRpcBridgePath.FullName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                })!;
                Plugin.Log.Info($"Wine RPC Bridge started (PID: {rpcBridgeProcess.Id}).");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Failed to start Wine RPC Bridge.");
            }
        }
        public void SetPresence(DiscordRPC.RichPresence presence)
        {
            CreateClient();
            rpcClient.SetPresence(presence);
        }
        public void ClearPresence()
        {
            CreateClient();
            rpcClient.ClearPresence();
        }
        public void UpdatePresenceDetails(string details)
        {
            CreateClient();
            rpcClient.UpdateDetails(details);
        }
        public void UpdatePresenceStartTime(DateTime startTime)
        {
            CreateClient();
            rpcClient.UpdateStartTime(startTime);
        }
        public void Dispose()
        {
            if (rpcBridgeProcess != null)
            {
                rpcBridgeProcess.Dispose();
                Plugin.Log.Info("Disposed Wine RPC Bridge process.");
            }

            rpcClient.Dispose();
        }
    }
}
