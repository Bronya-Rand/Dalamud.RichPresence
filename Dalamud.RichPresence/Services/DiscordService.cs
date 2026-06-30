using Dalamud.RichPresence.Discord;
using Dalamud.Utility;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;
using System;

namespace Dalamud.RichPresence.Services
{
    internal class DiscordService : IDisposable
    {
        private const string DISCORD_CLIENT_ID = "478143453536976896";
        private DiscordRpcClient RpcClient = null!;

        private readonly Configuration configuration;

        public DiscordService(Plugin plugin)
        {
            configuration = plugin.Configuration;

            CreateClient();
        }
        private void CreateClient()
        {
            if (RpcClient == null || RpcClient.IsDisposed)
            {
                // Under Wine (Linux/macOS) the default named-pipe transport cannot reach the
                // host's Discord socket, so connect to it directly over a Unix domain socket.
                INamedPipeClient? namedPipe = null;
                if (Util.IsWine() && configuration.ConnectDirectlyOnWine)
                {
                    namedPipe = new UnixSocketNamedPipeClient();
                }

                RpcClient = new DiscordRpcClient(
                    DISCORD_CLIENT_ID,
                    pipe: -1,
                    logger: new ConsoleLogger { Level = LogLevel.Warning },
                    autoEvents: true,
                    client: namedPipe)
                {
                    SkipIdenticalPresence = true,
                };
                RpcClient.OnPresenceUpdate += (sender, e) => { Plugin.Log.Debug($"Received Presence Update: {e.Presence}"); };
            }

            if (!RpcClient.IsInitialized)
                RpcClient.Initialize();
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
            RpcClient.Dispose();
        }
    }
}
