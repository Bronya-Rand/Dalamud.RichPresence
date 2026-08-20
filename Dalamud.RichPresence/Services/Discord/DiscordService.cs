using System;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.RichPresence.Helpers;
using Dalamud.Utility;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Services.Discord
{
    internal class DiscordService : IDisposable
    {
        private readonly Configuration configuration;
        private const string DiscordClientId = "478143453536976896";

        private DiscordRpcClient RpcClient = null!;
        private DiscordRPC.RichPresence? lastPresence;
        private volatile bool TcpBridgeNotificationShown = false;

        public DiscordService(Configuration configuration)
        {
            this.configuration = configuration;
            CreateClient();
        }

        private void CreateClient()
        {
            if (RpcClient == null || RpcClient.IsDisposed)
            {
                INamedPipeClient? rpcTransport = null;
                if (Util.IsWine())
                {
                    if (AfUnixHelper.IsAfUnixSupported())
                        rpcTransport = new DiscordUnixSocket();
                    else
                    {
                        if (configuration.UseCustomRpcTcpBridgePort)
                            rpcTransport = new DiscordTcpSocket(port: configuration.RpcTcpBridgePort);
                        else
                            rpcTransport = new DiscordTcpSocket();
                        if (!TcpBridgeNotificationShown)
                        {
                            TcpBridgeNotificationShown = true;
                            Plugin.NotificationManager.AddNotification(new Notification
                            {
                                Title = "RPC Bridge Required",
                                Content = Constants.RPCTCPBridgeWarning,
                                Type = NotificationType.Warning,
                            });
                        }
                    }
                }

                RpcClient = new DiscordRpcClient(DiscordClientId, client: rpcTransport)
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
            if (RpcClient == null) return;
            lastPresence = presence;
            RpcClient.SetPresence(presence);
        }
        public void ClearPresence()
        {
            CreateClient();
            if (RpcClient == null) return;
            RpcClient.ClearPresence();
        }
        public void UpdatePresenceDetails(string details)
        {
            CreateClient();
            if (RpcClient == null) return;
            RpcClient.UpdateDetails(details);
        }
        public void UpdatePresenceStartTime(DateTime startTime)
        {
            CreateClient();
            if (RpcClient == null) return;
            RpcClient.UpdateStartTime(startTime);
        }
        public void Dispose()
        {
            if (RpcClient == null) return;
            RpcClient.Dispose();
        }
        public void ReloadClient()
        {
            Dispose();
            CreateClient();
        }
    }
}
