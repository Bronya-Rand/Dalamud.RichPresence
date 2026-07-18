using System;
using System.Net.Sockets;
using Dalamud.RichPresence.Helpers;
using Dalamud.Utility;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Services.Discord
{
    /// <summary>
    /// Represents a Unix socket connection to Discord for IPC communication.
    /// </summary>
    internal class DiscordUnixSocket : INamedPipeClient
    {
        private Socket? socket;
        private NetworkStream? networkStream;
        private string? socketPath;
        private int connectedPipe;

        public ILogger Logger { get; set; } = null!;

        public int ConnectedPipe => connectedPipe;

        public bool IsConnected => GetIsConnected();

        public void Close() => Dispose();

        public bool Connect(int pipe)
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

                networkStream = new NetworkStream(socket, false);
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

        public void Dispose()
        {
            networkStream?.Dispose();
            socket?.Dispose();
            networkStream = null;
            socket = null;
            socketPath = null;
            connectedPipe = -1;
        }

        public bool ReadFrame(out PipeFrame frame)
        {
            frame = default;
            if (socket == null || networkStream == null || !IsConnected) return false;

            //Plugin.Log.Debug("Attempting to read frame from Discord socket...");
            try
            {
                if (socket.Available < 8) return false;

                // Read header (8 bytes: 4 bytes opcode, 4 bytes length)
                byte[] header = new byte[8];
                int bytesRead = 0;
                while (bytesRead < 8)
                {
                    int read = networkStream.Read(header, bytesRead, 8 - bytesRead);
                    if (read == 0) return false; // Connection closed
                    bytesRead += read;
                }

                uint opVal = BitConverter.ToUInt32(header, 0);
                uint length = BitConverter.ToUInt32(header, 4);

                // Validate length
                if (length > PipeFrame.MAX_SIZE)
                {
                    Plugin.Log.Error($"Received frame with payload size {length} exceeding maximum allowed size of {PipeFrame.MAX_SIZE} bytes.");
                    return false;
                }

                // Read payload
                byte[] data = new byte[length];
                bytesRead = 0;
                while (bytesRead < length)
                {
                    int read = networkStream.Read(data, bytesRead, (int)length - bytesRead);
                    if (read == 0) return false; // Connection closed
                    bytesRead += read;
                }

                frame = new PipeFrame
                {
                    Opcode = (Opcode)opVal,
                    Data = data,
                };
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error reading frame from Discord socket: {ex.Message}");
                return false;
            }
        }

        public bool WriteFrame(PipeFrame frame)
        {
            if (socket == null || networkStream == null || !IsConnected) return false;
            //Plugin.Log.Debug($"WriteFrame called, opcode={frame.Opcode}, length={frame.Data?.Length ?? 0}");

            // Validate frame size
            if (frame.Length > PipeFrame.MAX_SIZE)
            {
                Plugin.Log.Error($"Payload size {frame.Length} exceeds maximum allowed size of {PipeFrame.MAX_SIZE} bytes.");
                return false;
            }

            try
            {
                byte[] buffer = new byte[8 + frame.Data.Length];

                // Write opcode (4 byte uint)
                byte[] opcodeBytes = BitConverter.GetBytes((uint)frame.Opcode);
                Buffer.BlockCopy(opcodeBytes, 0, buffer, 0, 4);

                // Write data length (4 byte)
                byte[] lengthBytes = BitConverter.GetBytes(frame.Length);
                Buffer.BlockCopy(lengthBytes, 0, buffer, 4, 4);

                // Write data
                Buffer.BlockCopy(frame.Data, 0, buffer, 8, frame.Data.Length);

                // Send buffer to Discord socket
                networkStream.Write(buffer, 0, buffer.Length);
                networkStream.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error writing frame to Discord socket: {ex.Message}");
                return false;
            }
        }
        private bool GetIsConnected()
        {
            if (socket == null || !socket.Connected)
                return false;

            // Check if the socket is still connected by polling
            try
            {
                if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0)
                {
                    if (connectedPipe >= 0)
                        Plugin.Log.Info($"Discord socket (pipe {connectedPipe}) disconnected (remote close detected).");
                    connectedPipe = -1;
                    socketPath = null;
                    return false;
                }
                return true;
            }
            catch (SocketException e)
            {
                Plugin.Log.Error($"Error checking Discord socket connection (pipe {connectedPipe}): {e.Message}");
                return false;
            }
        }
    }
}
