using Microsoft.AspNetCore.Builder;

namespace Credfeto.Cache.Proxy.Server.Helpers;

internal static partial class Endpoints
{
    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        return app.ConfigureTestEndpoints();
    }
}
