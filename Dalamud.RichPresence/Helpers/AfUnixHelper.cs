using System;
using System.Net.Sockets;

namespace Dalamud.RichPresence.Helpers
{
    internal static class AfUnixHelper
    {
        private static bool? _afUnixSupported;

        public static bool IsAfUnixSupported()
        {
            if (_afUnixSupported.HasValue) return _afUnixSupported.Value;

            try
            {
                using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                _afUnixSupported = true;
            }
            catch (SocketException)
            {
                Plugin.Log.Warning("AF_UNIX is not supported on this platform.");
                _afUnixSupported = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning(ex, "AF_UNIX probe failed");
                _afUnixSupported = false;
            }

            return _afUnixSupported.Value;
        }
    }
}