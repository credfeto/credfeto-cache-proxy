using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Middleware;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Middleware;

public sealed class ServerHeaderMiddlewareTests : TestBase
{
    [Fact]
    public async Task XServerHeaderIsSetToMachineName()
    {
        DefaultHttpContext context = new();

        TrackingResponseFeature trackingFeature = new();
        context.Features.Set<IHttpResponseFeature>(trackingFeature);

        bool nextCalled = false;

        ServerHeaderMiddleware middleware = new();

        await middleware.InvokeAsync(
            context,
            async ctx =>
            {
                nextCalled = true;
                await trackingFeature.FireOnStartingAsync();
            }
        );

        Assert.Equal(expected: Environment.MachineName, actual: context.Response.Headers["X-Server"].ToString());
        Assert.True(nextCalled, userMessage: "next delegate was not called");
    }

    private sealed class TrackingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = [];

        public int StatusCode { get; set; } = 200;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state)
        {
            this._onStartingCallbacks.Add((callback, state));
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            // Not needed for these tests
        }

        public async Task FireOnStartingAsync()
        {
            foreach ((Func<object, Task> callback, object state) in this._onStartingCallbacks)
            {
                await callback(state);
            }
        }
    }
}
