using System;
using System.Diagnostics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;
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
        
        private DiscordRpcClient? rpcClient;
        private DiscordRPC.RichPresence? lastPresence;
        private volatile bool isFirstStart = true;
        private volatile bool tcpBridgeNotificationShown;

        public DiscordService(Configuration configuration)
        {
            this.configuration = configuration;
            EnsureClient();
        }

        private DiscordRpcClient EnsureClient()
        {
            var client = rpcClient;
            if (client == null || client.IsDisposed)
            {
                INamedPipeClient? rpcTransport = null;
                if (Util.IsWine())
                {
                    if (AfUnixHelper.IsAfUnixSupported())
                        rpcTransport = new DiscordUnixSocket();
                    else
                    {
                        rpcTransport = configuration.UseCustomRpcTcpBridgePort
                            ? new DiscordTcpSocket(port: configuration.RpcTcpBridgePort)
                            : new DiscordTcpSocket();

                        if (!tcpBridgeNotificationShown)
                        {
                            tcpBridgeNotificationShown = true;
                            var notification = Plugin.NotificationManager.AddNotification(new Notification
                            {
                                Title = "RPC Bridge Required",
                                Content = Constants.RpctcpBridgeWarning,
                                Type = NotificationType.Warning,
                            });
                            notification.DrawActions += NotificationActions;
                        }
                    }
                }
                
                client = new DiscordRpcClient(DiscordClientId, client: rpcTransport)
                {
                    Logger = new RpcLogger { Level = LogLevel.Trace },
                };

                var created = client;
                created.OnReady += (_, e) =>
                {
                    Plugin.Log.Info($"Discord RPC ready: {e.User?.Username}");

                    // Re-send the last presence on reconnect so the new Discord
                    // client immediately shows current game status.
                    if (lastPresence != null && !isFirstStart)
                    {
                        Plugin.Log.Info("Re-sending presence after reconnect.");
                        created.SetPresence(lastPresence);
                    }

                    if (isFirstStart)
                        isFirstStart = false;
                };
                created.OnPresenceUpdate += (_, _) => { Plugin.Log.Verbose($"Received Presence Update"); };

                rpcClient = created;
            }
            
            if (!client.IsInitialized)
                client.Initialize();
            
            return client;
        }

        public void SetPresence(DiscordRPC.RichPresence presence)
        {
            var client = EnsureClient();
            lastPresence = presence;
            client.SetPresence(presence);
        }

        public void ClearPresence() => EnsureClient().ClearPresence();
        public void UpdatePresenceDetails(string details) => EnsureClient().UpdateDetails(details);

        public void UpdatePresenceStartTime(DateTime startTime) => EnsureClient().UpdateStartTime(startTime);

        public void Dispose()
        {
            rpcClient?.Dispose();
            rpcClient = null;
        }

        public void ReloadClient()
        {
            Dispose();
            EnsureClient();
        }

        private static void NotificationActions(INotificationDrawArgs args)
        {
            if (ImGui.Button("Download RPC Bridge"))
                Process.Start(new ProcessStartInfo
                {
                    FileName = Constants.RpctcpBridgeDownloadUrl,
                    UseShellExecute = true
                });
            if (ImGui.Button("Instructions"))
                Process.Start(new ProcessStartInfo
                {
                    FileName = Constants.RpctcpBridgeReadmeUrl,
                    UseShellExecute = true
                });
        }
    }
}
