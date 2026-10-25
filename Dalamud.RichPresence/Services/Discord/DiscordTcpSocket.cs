using System;
using System.Net.Sockets;

namespace Dalamud.RichPresence.Services.Discord
{
    internal class DiscordTcpSocket(string host = "localhost", int port = 2026) : DiscordUnixStream
    {
        private TcpClient? client;
        private int Port { get; } = port;

        public override bool Connect(int pipe)
        {
            try
            {
                // Close any existing connection
                Close();

                client = new TcpClient();
                client.Connect(host, Port);
                Stream = client.GetStream();
                ConnectedPipe = pipe >= 0 ? pipe : 0;

                Plugin.Log.Info($"Connected to Discord TCP bridge at {host}:{Port}");
                return true;
            }
            catch (SocketException)
            {
                return false;
            } // Ignore socket exceptions,
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"Failed to connect to Discord TCP bridge at {host}:{Port}");
                Close();
                return false;
            }
        }

        public override void Dispose()
        {
            Stream?.Dispose();
            Stream = null;

            client?.Dispose();
            client = null;

            ConnectedPipe = -1;
        }
    }
}