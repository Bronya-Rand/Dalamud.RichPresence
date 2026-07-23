using System;
using System.Net.Sockets;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Services.Discord
{
    internal abstract class DiscordUnixStream : INamedPipeClient
    {
        protected NetworkStream? Stream;
        protected int connectedPipe = -1;

        public ILogger Logger { get; set; } = null!;
        public int ConnectedPipe => connectedPipe;
        public virtual bool IsConnected => GetIsConnected();
        protected virtual bool HasFullHeaderAvailable() => Stream?.Socket.Available >= 8;
        public abstract bool Connect(int pipe);
        public abstract void Dispose();
        public void Close() => Dispose();

        public bool ReadFrame(out PipeFrame frame)
        {
            frame = default;
            if (Stream == null || !IsConnected) return false;

            try
            {
                if (!HasFullHeaderAvailable()) return false;

                // Read header (8 bytes: 4 bytes opcode, 4 bytes length)
                byte[] header = new byte[8];
                int bytesRead = 0;
                while (bytesRead < 8)
                {
                    int read = Stream.Read(header, bytesRead, 8 - bytesRead);
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
                    int read = Stream.Read(data, bytesRead, (int)length - bytesRead);
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
            catch (Exception e)
            {
                Plugin.Log.Error($"Error reading frame from Discord socket: {e.Message}");
                return false;
            }
        }
        public bool WriteFrame(PipeFrame frame)
        {
            if (Stream == null || !IsConnected) return false;

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
                Stream.Write(buffer, 0, buffer.Length);
                Stream.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error writing frame to Discord socket: {ex.Message}");
                return false;
            }
        }

        protected virtual bool GetIsConnected()
        {
            if (Stream?.Socket is not { Connected: true } socket)
                return false;

            try
            {
                if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0)
                {
                    if (connectedPipe >= 0)
                        Plugin.Log.Info($"Discord connection (pipe {connectedPipe}) disconnected (remote close detected).");
                    connectedPipe = -1;
                    return false;
                }
                return true;
            }
            catch (SocketException e)
            {
                Plugin.Log.Error($"Error checking Discord connection (pipe {connectedPipe}): {e.Message}");
                return false;
            }
        }
    }
}
