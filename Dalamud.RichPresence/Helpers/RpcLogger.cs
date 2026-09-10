using DiscordRPC.Logging;

namespace Dalamud.RichPresence.Helpers
{
    internal class RpcLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Trace;

        public void Error(string message, params object[] args)
            => Plugin.Log.Error($"[DiscordRPC Library] {message}", args);

        public void Info(string message, params object[] args) =>
            Plugin.Log.Information($"[DiscordRPC Library] {message}", args);

        public void Trace(string message, params object[] args) =>
            Plugin.Log.Verbose($"[DiscordRPC Library] {message}", args);

        public void Warning(string message, params object[] args) =>
            Plugin.Log.Warning($"[DiscordRPC Library] {message}", args);
    }
}
