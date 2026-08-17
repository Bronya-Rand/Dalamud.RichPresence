namespace RichPresenceBridge
{
    internal enum LogLevel { Debug, Info, Warning, Error }
    internal static class DebugLog
    {
        private static readonly string LogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "RichPresence.log"
        );
        private static readonly Lock LogLock = new();
        private static bool EnableDebug { get; set; } = false;

        public static void Init(bool debug)
        {
            EnableDebug = debug;
        }
        public static void Log(LogLevel level, string message)
        {
            if (level == LogLevel.Debug && !EnableDebug) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var formattedMessage = $"[{timestamp}] [{level.ToString().ToUpperInvariant()}] {message}";

            lock (LogLock)
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = level switch
                {
                    LogLevel.Debug => ConsoleColor.Gray,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };
                Console.WriteLine(formattedMessage);
                Console.ForegroundColor = prevColor;

                try { File.AppendAllText(LogPath, $"{formattedMessage}{Environment.NewLine}"); }
                catch { /* Ignore file write errors */ }
            }
        }
        public static void Debug(string message) => Log(LogLevel.Debug, message);
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(Exception ex, string message)
        {
            var details = EnableDebug
                ? $"{message}: {ex}"
                : $"{message}: {ex.Message}";

            Log(LogLevel.Error, details);
        }
        public static void Error(Exception ex) => Error(ex, ex.Message);
        public static void Error(string message) => Log(LogLevel.Error, message);
        public static void Flush() => File.WriteAllText(LogPath, string.Empty);
    }
}
