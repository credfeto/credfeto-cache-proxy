using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Credfeto.Cache.Proxy.Server.Extensions;

internal static class HttpContextExtensions
{
    public static ProductInfoHeaderValue? GetUserAgent(this HttpContext context)
    {
        string? ua = context.Request.Headers.UserAgent;

        if (string.IsNullOrWhiteSpace(ua))
        {
            return null;
        }

        if (ProductInfoHeaderValue.TryParse(input: ua, out ProductInfoHeaderValue? userAgent))
        {
            return userAgent;
        }

        return null;
    }
}