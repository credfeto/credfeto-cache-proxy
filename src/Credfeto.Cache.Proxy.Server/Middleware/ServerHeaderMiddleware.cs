using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Credfeto.Cache.Proxy.Server.Middleware;

public sealed class ServerHeaderMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers["X-Server"] = Environment.MachineName;

        return next(context);
    }
}
