using Dalamud.Utility;
using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace Dalamud.RichPresence.Services
{
    internal class DiscordService : IDisposable
    {
        private static DirectoryInfo WINE_RPC_BRIDGE_PATH => new(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, "Resources/binaries", "WineRPCBridge.exe"));

        private const string DISCORD_CLIENT_ID = "478143453536976896";
        private DiscordRpcClient RpcClient = null!;
        private Process? rpcBridgeProcess;

        private readonly Configuration configuration;

        public DiscordService(Plugin plugin)
        {
            configuration = plugin.Configuration;

            CreateClient();

            if (Util.IsWine() && configuration.RPCBridgeEnabled)
            {
                StartWineRPCBridge();
            }
        }
        private void CreateClient()
        {
            if (RpcClient == null || RpcClient.IsDisposed)
            {
                RpcClient = new DiscordRpcClient(DISCORD_CLIENT_ID)
                {
                    SkipIdenticalPresence = true,

                    Logger = new ConsoleLogger { Level = LogLevel.Warning }
                };
                RpcClient.OnPresenceUpdate += (sender, e) => { Plugin.Log.Debug($"Received Presence Update: {e.Presence}"); };
            }

            if (!RpcClient.IsInitialized)
                RpcClient.Initialize();
        }
        public void StartWineRPCBridge()
        {
            try
            {
                // Find existing bridge process.
                var wineBridge = Process.GetProcessesByName(WINE_RPC_BRIDGE_PATH.Name);
                if (wineBridge != null && wineBridge.Length > 0)
                {
                    Plugin.Log.Info($"Wine RPC Bridge found (PID: {wineBridge[0].Id}), skipping creation.");
                    rpcBridgeProcess = wineBridge[0];
                    return;
                }

                // Start the bridge.
                Plugin.Log.Info($"Starting Wine RPC Bridge: {WINE_RPC_BRIDGE_PATH}");
                rpcBridgeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = WINE_RPC_BRIDGE_PATH.FullName,
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
            RpcClient.SetPresence(presence);
        }
        public void ClearPresence()
        {
            CreateClient();
            RpcClient.ClearPresence();
        }
        public void UpdatePresenceDetails(string details)
        {
            CreateClient();
            RpcClient.UpdateDetails(details);
        }
        public void UpdatePresenceStartTime(DateTime startTime)
        {
            CreateClient();
            RpcClient.UpdateStartTime(startTime);
        }
        public void Dispose()
        {
            if (rpcBridgeProcess != null)
            {
                rpcBridgeProcess.Dispose();
                Plugin.Log.Info("Disposed Wine RPC Bridge process.");
            }

            RpcClient.Dispose();
        }
    }
}
