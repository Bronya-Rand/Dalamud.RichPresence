using System;
using System.Net.Sockets;

namespace Dalamud.RichPresence.Helpers
{
    internal static class AfUnixHelper
    {
        private static bool? AFUnixSupported;

        public static bool IsAfUnixSupported()
        {
            if (AFUnixSupported.HasValue) return AFUnixSupported.Value;

            try
            {
                using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                AFUnixSupported = true;
            }
            catch (SocketException)
            {
                Plugin.Log.Warning("AF_UNIX is not supported on this platform.");
                AFUnixSupported = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning(ex, "AF_UNIX probe failed");
                AFUnixSupported = false;
            }

            return AFUnixSupported.Value;
        }
    }
}
