using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            CreateClient();
        }
        
        [MemberNotNull(nameof(rpcClient))]
        private void CreateClient()
        {
            if (rpcClient == null || rpcClient.IsDisposed)
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

                rpcClient = new DiscordRpcClient(DiscordClientId, client: rpcTransport)
                {
                    Logger = new RpcLogger { Level = LogLevel.Trace },
                };
                rpcClient.OnReady += (_, e) =>
                {
                    Plugin.Log.Info($"Discord RPC ready: {e.User?.Username}");

                    // Re-send the last presence on reconnect so the new Discord
                    // client immediately shows current game status.
                    if (lastPresence != null && !isFirstStart)
                    {
                        Plugin.Log.Info("Re-sending presence after reconnect.");
                        rpcClient.SetPresence(lastPresence);
                    }

                    if (isFirstStart)
                        isFirstStart = false;
                };
                rpcClient.OnPresenceUpdate += (_, _) => { Plugin.Log.Verbose($"Received Presence Update"); };
            }

            if (!rpcClient.IsInitialized)
                rpcClient.Initialize();
        }

        public void SetPresence(DiscordRPC.RichPresence presence)
        {
            CreateClient();
            lastPresence = presence;
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

        public void Dispose() => rpcClient?.Dispose();

        public void ReloadClient()
        {
            Dispose();
            CreateClient();
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
