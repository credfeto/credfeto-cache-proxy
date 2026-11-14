using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Cache.Proxy.Server.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Cache.Proxy.Server.Tests.Helpers;

public sealed class SelfTestTests : LoggingFolderCleanupTestBase
{
    public SelfTestTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void IsSelfTest_WhenSwitchIsOnlyArgument_ReturnsTrue()
    {
        Assert.True(SelfTest.IsSelfTest(["--self-test"]), userMessage: "Should detect self-test switch");
    }

    [Fact]
    public void IsSelfTest_WhenSwitchFollowsOtherArguments_ReturnsTrue()
    {
        Assert.True(SelfTest.IsSelfTest(["--verbose", "--self-test"]), userMessage: "Should detect self-test switch");
    }

    [Fact]
    public void IsSelfTest_WhenNoArguments_ReturnsFalse()
    {
        Assert.False(SelfTest.IsSelfTest([]), userMessage: "Should not detect self-test switch");
    }

    [Fact]
    public void IsSelfTest_WhenHealthCheck_ReturnsFalse()
    {
        Assert.False(
            SelfTest.IsSelfTest(["--health-check", "http://127.0.0.1:8080/ping"]),
            userMessage: "Should not detect self-test switch"
        );
    }

    [Fact]
    public void IsSelfTest_WhenSwitchHasDifferentCase_ReturnsFalse()
    {
        Assert.False(SelfTest.IsSelfTest(["--SELF-TEST"]), userMessage: "Should not detect self-test switch");
    }

    [Fact]
    public async Task RunAsync_WithValidConfiguration_ReturnsZeroAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        string storage = Path.Combine(path1: this.TempFolder, path2: "data");
        string settings = $$"""{ "Proxy": { "Storage": "{{JsonEncodedText.Encode(storage)}}", "Sites": [] } }""";

        await File.WriteAllTextAsync(
            Path.Combine(path1: this.TempFolder, path2: "appsettings.json"),
            contents: settings,
            cancellationToken: cancellationToken
        );

        await using (StringWriter output = new())
        {
            int exitCode = await SelfTest.RunAsync(
                configPath: this.TempFolder,
                output: output,
                cancellationToken: cancellationToken
            );

            Assert.Equal(expected: 0, actual: exitCode);
            Assert.Equal(expected: "Self-test passed" + Environment.NewLine, actual: output.ToString());
        }
    }

    [Fact]
    public async Task RunAsync_WithMissingConfiguration_ReturnsNonZeroWithSingleLineReasonAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        await using (StringWriter output = new())
        {
            int exitCode = await SelfTest.RunAsync(
                configPath: this.TempFolder,
                output: output,
                cancellationToken: cancellationToken
            );

            Assert.NotEqual(expected: 0, actual: exitCode);

            string message = output.ToString();
            Assert.StartsWith(
                expectedStartString: "Self-test failed: ",
                actualString: message,
                comparisonType: StringComparison.Ordinal
            );
            Assert.Single(
                message.Split(separator: Environment.NewLine, options: StringSplitOptions.RemoveEmptyEntries)
            );
        }
    }
}
