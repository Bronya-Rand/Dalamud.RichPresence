using System;
using Dalamud.Utility;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Services.Discord
{
    internal class DiscordService : IDisposable
    {
        private const string DiscordClientId = "478143453536976896";
        private DiscordRpcClient RpcClient = null!;
        private DiscordRPC.RichPresence? lastPresence;

        public DiscordService() => CreateClient();
        private void CreateClient()
        {
            if (RpcClient == null || RpcClient.IsDisposed)
            {
                INamedPipeClient? unixSocket = null;
                if (Util.IsWine())
                    unixSocket = new DiscordUnixSocket();

                RpcClient = new DiscordRpcClient(DiscordClientId, client: unixSocket)
                {
                    SkipIdenticalPresence = true,
                    Logger = new ConsoleLogger { Level = LogLevel.Warning }
                };
                RpcClient.OnReady += (sender, e) =>
                {
                    Plugin.Log.Info($"Discord RPC ready: {e.User?.Username}");

                    // Re-send the last presence on reconnect so the new Discord
                    // client immediately shows current game status.
                    if (lastPresence != null)
                    {
                        Plugin.Log.Info("Re-sending presence after reconnect.");
                        RpcClient.SetPresence(lastPresence);
                    }
                };
                RpcClient.OnPresenceUpdate += (sender, e) => { Plugin.Log.Debug($"Received Presence Update: {e.Presence}"); };
            }

            if (!RpcClient.IsInitialized)
                RpcClient.Initialize();
        }
        public void SetPresence(DiscordRPC.RichPresence presence)
        {
            CreateClient();
            lastPresence = presence;
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
        public void Dispose() => RpcClient.Dispose();
    }
}
