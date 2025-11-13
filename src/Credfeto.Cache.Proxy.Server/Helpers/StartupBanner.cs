using System;
using Figgle;

namespace Credfeto.Cache.Proxy.Server.Helpers;

// https://www.figlet.org/examples.html
[GenerateFiggleText("Banner", "basic", "Cache Proxy")]
internal static partial class StartupBanner
{
    public static void Show()
    {
        Console.WriteLine(Banner);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Starting version " + VersionInformation.Version + "...");
    }
}
