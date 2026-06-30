using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Discord
{
    /// <summary>
    /// A Discord IPC transport that talks directly to Discord's Unix domain socket.
    /// <para>
    /// On Linux/macOS the game runs through Wine, where the default
    /// <c>ManagedNamedPipeClient</c> tries to open the Windows named pipe
    /// <c>\\.\pipe\discord-ipc-N</c>, which Wine does not bridge to the host's
    /// Discord socket. This client instead connects to the host
    /// <c>discord-ipc-N</c> Unix socket through Wine's AF_UNIX support, and is
    /// aware of the sandboxed socket locations used by Flatpak and Snap.
    /// </para>
    /// <para>
    /// This replaces the previous external <c>WineRPCBridge.exe</c> helper, which
    /// relied on 32-bit Linux syscalls that no longer work on modern 64-bit-only
    /// (new-WoW64) Wine builds.
    /// </para>
    /// </summary>
    internal sealed class UnixSocketNamedPipeClient : INamedPipeClient
    {
        // Sub-directories (relative to a runtime dir) where Discord may place its socket.
        private static readonly string[] SandboxSubdirectories =
        {
            "",                                  // native install
            "snap.discord/",                     // Snap
            "app/com.discordapp.Discord/",       // Flatpak (stable)
            "app/com.discordapp.DiscordCanary/", // Flatpak (canary)
            "app/com.discordapp.DiscordPTB/",    // Flatpak (PTB)
        };

        private const uint MaxFrameLength = 64 * 1024;

        private readonly object frameQueueLock = new();
        private readonly Queue<PipeFrame> frameQueue = new();

        private Socket? socket;
        private NetworkStream? stream;
        private Thread? readThread;
        private volatile bool isClosed;
        private int connectedPipe = -1;

        public ILogger Logger { get; set; } = null!;

        public bool IsConnected => !isClosed && socket is { Connected: true };

        public int ConnectedPipe => connectedPipe;

        public bool Connect(int pipe)
        {
            // Reset any previous state so the client can be reused on reconnect.
            Close();
            isClosed = false;

            if (pipe < 0)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (TryConnectPipe(i))
                        return true;
                }

                return false;
            }

            return TryConnectPipe(pipe);
        }

        private bool TryConnectPipe(int pipe)
        {
            foreach (var path in GetSocketPaths(pipe))
            {
                Socket? candidate = null;
                try
                {
                    candidate = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    candidate.Connect(new UnixDomainSocketEndPoint(path));

                    socket = candidate;
                    stream = new NetworkStream(candidate, ownsSocket: false);
                    connectedPipe = pipe;

                    Logger?.Info("Connected to Discord IPC socket at {0}", path);

                    readThread = new Thread(ReadLoop)
                    {
                        Name = "RichPresence Discord IPC Reader",
                        IsBackground = true,
                    };
                    readThread.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger?.Trace("Failed to connect to {0}: {1}", path, ex.Message);
                    candidate?.Dispose();
                }
            }

            return false;
        }

        private void ReadLoop()
        {
            var localStream = stream;
            if (localStream is null)
                return;

            var header = new byte[8];
            try
            {
                while (!isClosed)
                {
                    if (!ReadExact(localStream, header, 8))
                        break;

                    var opcode = BitConverter.ToUInt32(header, 0);
                    var length = BitConverter.ToUInt32(header, 4);

                    if (length > MaxFrameLength)
                    {
                        Logger?.Error("Discord IPC frame too large ({0} bytes), aborting connection.", length);
                        break;
                    }

                    var data = new byte[length];
                    if (length > 0 && !ReadExact(localStream, data, (int)length))
                        break;

                    var frame = new PipeFrame
                    {
                        Opcode = (Opcode)opcode,
                        Data = data,
                    };

                    lock (frameQueueLock)
                        frameQueue.Enqueue(frame);
                }
            }
            catch (Exception ex)
            {
                if (!isClosed)
                    Logger?.Trace("Discord IPC read loop ended: {0}", ex.Message);
            }
            finally
            {
                // Signal disconnect so the RPC connection can attempt to reconnect,
                // but only if this thread still owns the active stream (avoids a
                // stale read thread clobbering a freshly re-established connection).
                if (ReferenceEquals(stream, localStream))
                    isClosed = true;
            }
        }

        public bool ReadFrame(out PipeFrame frame)
        {
            lock (frameQueueLock)
            {
                if (frameQueue.Count == 0)
                {
                    frame = default;
                    return false;
                }

                frame = frameQueue.Dequeue();
                return true;
            }
        }

        public bool WriteFrame(PipeFrame frame)
        {
            if (isClosed || stream is null)
                return false;

            try
            {
                frame.WriteStream(stream);
                stream.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Logger?.Error("Failed to write Discord IPC frame: {0}", ex.Message);
                isClosed = true;
                return false;
            }
        }

        public void Close()
        {
            isClosed = true;

            try { stream?.Dispose(); } catch { /* ignored */ }
            try { socket?.Dispose(); } catch { /* ignored */ }

            stream = null;
            socket = null;
            connectedPipe = -1;

            lock (frameQueueLock)
                frameQueue.Clear();
        }

        public void Dispose()
        {
            Close();
        }

        private static bool ReadExact(Stream source, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = source.Read(buffer, offset, count - offset);
                if (read <= 0)
                    return false;

                offset += read;
            }

            return true;
        }

        private static IEnumerable<string> GetSocketPaths(int pipe)
        {
            foreach (var runtimeDir in GetRuntimeDirectories())
            {
                foreach (var subdir in SandboxSubdirectories)
                    yield return $"{runtimeDir}/{subdir}discord-ipc-{pipe}";
            }
        }

        private static IEnumerable<string> GetRuntimeDirectories()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var dir in EnumerateRuntimeDirectoryCandidates())
            {
                if (string.IsNullOrEmpty(dir))
                    continue;

                var normalized = dir.Replace('\\', '/').TrimEnd('/');
                if (normalized.Length != 0 && seen.Add(normalized))
                    yield return normalized;
            }
        }

        private static IEnumerable<string?> EnumerateRuntimeDirectoryCandidates()
        {
            yield return Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            yield return Environment.GetEnvironmentVariable("TMPDIR");
            yield return Environment.GetEnvironmentVariable("TMP");
            yield return Environment.GetEnvironmentVariable("TEMP");
            yield return "/tmp";

            // If XDG_RUNTIME_DIR was not propagated into the Wine environment, try to
            // discover the per-user runtime directory through Wine's drive mapping.
            var discovered = new List<string>();
            try
            {
                foreach (var dir in Directory.GetDirectories(@"Z:\run\user"))
                    discovered.Add("/run/user/" + Path.GetFileName(dir));
            }
            catch
            {
                // Not running under Wine, or the path is unavailable; ignore.
            }

            foreach (var dir in discovered)
                yield return dir;
        }
    }
}
