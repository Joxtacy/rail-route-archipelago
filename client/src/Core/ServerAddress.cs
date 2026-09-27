using System;
using System.Globalization;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// The "server" setting: <c>host:port</c>, a bare <c>host</c> (the Archipelago default port 38281), or a
    /// <c>ws://</c>/<c>wss://</c> URI (38281 when it names no port). Anything else is invalid.
    /// </summary>
    public sealed class ServerAddress
    {
        public const int DefaultPort = 38281;

        private ServerAddress(string host, int port, string scheme)
        {
            Host = host;
            Port = port;
            Scheme = scheme;
        }

        public string Host { get; }

        public int Port { get; }

        /// <summary>"ws" or "wss" for a URI, null for <c>host:port</c> and a bare host.</summary>
        public string Scheme { get; }

        public bool IsUri => Scheme != null;

        public Uri ToUri() => new UriBuilder(Scheme ?? "ws", Host, Port).Uri;

        public override string ToString() => IsUri ? Scheme + "://" + Host + ":" + Port : Host + ":" + Port;

        public static bool TryParse(string text, out ServerAddress address)
        {
            address = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            var rest = text.Trim();
            string scheme = null;
            var schemeEnd = rest.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0)
            {
                scheme = rest.Substring(0, schemeEnd).ToLowerInvariant();
                if (scheme != "ws" && scheme != "wss")
                {
                    return false;
                }
                // Only an authority, optionally with a trailing slash: no path, query or credentials.
                rest = rest.Substring(schemeEnd + 3);
                if (rest.EndsWith("/", StringComparison.Ordinal))
                {
                    rest = rest.Substring(0, rest.Length - 1);
                }
            }
            if (!TryParseAuthority(rest, out var host, out var port))
            {
                return false;
            }
            address = new ServerAddress(host, port, scheme);
            return true;
        }

        private static bool TryParseAuthority(string text, out string host, out int port)
        {
            host = text;
            port = DefaultPort;
            var colon = text.LastIndexOf(':');
            if (colon >= 0)
            {
                host = text.Substring(0, colon);
                if (!int.TryParse(text.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535)
                {
                    return false;
                }
            }
            var type = Uri.CheckHostName(host);
            return type == UriHostNameType.Dns || type == UriHostNameType.IPv4;
        }
    }
}
