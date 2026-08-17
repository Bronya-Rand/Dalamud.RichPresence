using System;
using System.Net.Sockets;

namespace Dalamud.RichPresence.Services.Discord
{
    internal class DiscordTcpSocket : DiscordUnixStream
    {
        private readonly string host;
        private TcpClient? client;
        public int Port { get; set; }

        public DiscordTcpSocket(string host = "localhost", int port = 2026)
        {
            this.host = host;
            Port = port;
        }

        public override bool Connect(int pipe)
        {
            try
            {
                // Close any existing connection
                Close();

                client = new TcpClient();
                client.Connect(host, Port);
                Stream = client.GetStream();
                connectedPipe = pipe >= 0 ? pipe : 0;

                Plugin.Log.Info($"Connected to Discord TCP bridge at {host}:{Port}");
                return true;
            }
            catch (SocketException) { return false; } // Ignore socket exceptions,
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

            connectedPipe = -1;
        }
    }
}
