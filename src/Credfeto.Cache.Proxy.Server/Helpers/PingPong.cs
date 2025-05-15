using Credfeto.Cache.Proxy.Models;

namespace Credfeto.Cache.Proxy.Server.Helpers;

internal static class PingPong
{
    public static PongDto Model { get; } = new("Pong!");
}
