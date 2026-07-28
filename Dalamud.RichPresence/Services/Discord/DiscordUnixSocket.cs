using System;
using System.Net.Sockets;
using Dalamud.RichPresence.Helpers;
using Dalamud.Utility;

namespace Dalamud.RichPresence.Services.Discord
{
    /// <summary>
    /// Represents a Unix socket connection to Discord for IPC communication.
    /// </summary>
    internal class DiscordUnixSocket : DiscordUnixStream
    {
        private Socket? socket;
        private string? socketPath;
        public override bool Connect(int pipe)
        {
            try
            {
                if (pipe < 0)
                {
                    // Find the first available Discord socket (0-9)
                    for (var i = 0; i < 10; i++)
                    {
                        var discordSocketPath = DiscordSocketResolver.FindSocket(i);
                        if (!discordSocketPath.IsNullOrEmpty())
                        {
                            socketPath = discordSocketPath;
                            connectedPipe = i;
                            break;
                        }
                    }
                }
                else
                {
                    // Use the specified pipe number
                    socketPath = DiscordSocketResolver.FindSocket(pipe);
                    connectedPipe = pipe;
                }
                if (socketPath.IsNullOrEmpty())
                    return false;

                // Create socket
                Plugin.Log.Info($"Attempting to connect to Discord socket at {socketPath} (pipe {connectedPipe})");
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Connect(new UnixDomainSocketEndPoint(socketPath));

                Stream = new NetworkStream(socket, false);
                Plugin.Log.Info($"Connected to Discord socket at {socketPath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to connect to Discord socket at {socketPath}: {ex.Message}");
                Dispose();
                return false;
            }
        }

        public override void Dispose()
        {
            socket?.Dispose();
            socket = null;
            socketPath = null;
            connectedPipe = -1;
        }
    }
}
