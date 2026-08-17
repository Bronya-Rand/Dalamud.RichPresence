/*
 * Rich Presence Bridge - Handles Discord RPC connections between XIV
 * and Discord. Mainly used for Wine/Proton without AF_UNIX support and
 * macOS users on XOM (XIV on Mac).
 */

using System.Net;
using System.Net.Sockets;
using Dalamud.RichPresence.Shared;

namespace RichPresenceBridge;

internal static class Program
{
    private const int DefaultPort = 2026;

    private static async Task Main(string[] args)
    {
        ParseArgurments(args, out var port, out var debug);

        // Setup logging for the socket resolver
        var socketResolver = new SocketResolver
        {
            LogCallback = DebugLog.Info,
            LogErrorCallback = (ex, message) =>
            {
                if (ex != null)
                    DebugLog.Error(ex, message);
                else
                    DebugLog.Error(message);
            },
            LogDebugCallback = DebugLog.Debug
        };

        // Allow for the user to specify a custom port from the config window
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        DebugLog.Info($"Listening on port {port}...");

        while (true)
        {
            try
            {
                // Wait for plugin to connect
                using var tcpClient = await listener.AcceptTcpClientAsync();
                DebugLog.Debug("Connected to RPC plugin.");

                // Find the Discord socket path
                string? socketPath = null;
                int? connectedPipe = null;
                for (int i = 0; i < 10; i++)
                {
                    var discordSocketPath = socketResolver.FindSocket(i);
                    if (!string.IsNullOrEmpty(discordSocketPath))
                    {
                        socketPath = discordSocketPath;
                        connectedPipe = i;
                        break;
                    }
                }
                if (socketPath == null || connectedPipe == null)
                {
                    DebugLog.Error("Could not find Discord socket path.");
                    continue;
                }

                // Connect to Discord
                var discordEndPoint = new UnixDomainSocketEndPoint(socketPath);
                using var unixSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await unixSocket.ConnectAsync(discordEndPoint);
                DebugLog.Info($"Connected to Discord socket at {socketPath} (pipe {connectedPipe}).");

                // Relay data between the plugin and Discord until one disconnects
                using var tcpStream = tcpClient.GetStream();
                using var unixStream = new NetworkStream(unixSocket, ownsSocket: false);

                using var cts = new CancellationTokenSource();
                var xivToDiscord = tcpStream.CopyToAsync(unixStream, cts.Token);
                var discordToXiv = unixStream.CopyToAsync(tcpStream, cts.Token);

                await Task.WhenAny(xivToDiscord, discordToXiv);
                await cts.CancelAsync();
            }
            catch (OperationCanceledException) { /* Ignore cancellation */ }
            catch (IOException) { DebugLog.Info("Disconnected from RPC plugin or Discord."); }
            catch (Exception ex)
            {
                DebugLog.Error(ex, "Unexpected bridge error");
            }
        }
    }
    private static void ParseArgurments(string[] args, out int port, out bool debug)
    {
        port = DefaultPort;
        debug = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedPort))
                    {
                        port = parsedPort;
                        i++; // Skip the next index
                    }
                    else
                    {
                        DebugLog.Warning("Invalid port specified. Using default port.");
                    }
                    break;
                case "--debug":
                case "-d":
                    debug = true;
                    break;

                case "--help":
                case "-h":
                    Console.WriteLine("Usage: RichPresenceBridge [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --port, -p <port>   Specify the port to listen on (default: 2026)");
                    Console.WriteLine("  --debug, -d         Enable debug logging");
                    Console.WriteLine("  --help, -h          Show this help message");
                    return;

                default:
                    DebugLog.Error($"Unknown argument: {args[i]}");
                    break;
            }
        }
        // Initialize the logging system with the debug flag
        DebugLog.Init(debug);
    }
}