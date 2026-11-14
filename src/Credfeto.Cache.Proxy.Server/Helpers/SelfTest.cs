using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Cache.Proxy.Server.Helpers;

public static class SelfTest
{
    private const string SELF_TEST_SWITCH = "--self-test";
    private const int SUCCESS = 0;
    private const int FAILURE = 1;

    public static bool IsSelfTest(IReadOnlyList<string> args)
    {
        return args.Contains(value: SELF_TEST_SWITCH, comparer: StringComparer.Ordinal);
    }

    public static async ValueTask<int> RunAsync(
        string configPath,
        TextWriter output,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await ServerStartup.VerifyStartupAsync(configPath);
            await output.WriteLineAsync("Self-test passed".AsMemory(), cancellationToken: cancellationToken);

            return SUCCESS;
        }
        catch (Exception exception)
        {
            await output.WriteLineAsync(FormatFailure(exception).AsMemory(), cancellationToken: cancellationToken);

            return FAILURE;
        }
    }

    private static string FormatFailure(Exception exception)
    {
        return $"Self-test failed: {exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}";
    }
}
