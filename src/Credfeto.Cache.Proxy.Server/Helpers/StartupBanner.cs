using System;

namespace Credfeto.Cache.Proxy.Server.Helpers;

internal static class StartupBanner
{
    public static void Show()
    {
        // Generated from https://fsymbols.com/generators/carty/
        const string banner =
            @"
░█████╗░░█████╗░░█████╗░██╗░░██╗███████╗  ██████╗░██████╗░░█████╗░██╗░░██╗██╗░░░██╗
██╔══██╗██╔══██╗██╔══██╗██║░░██║██╔════╝  ██╔══██╗██╔══██╗██╔══██╗╚██╗██╔╝╚██╗░██╔╝
██║░░╚═╝███████║██║░░╚═╝███████║█████╗░░  ██████╔╝██████╔╝██║░░██║░╚███╔╝░░╚████╔╝░
██║░░██╗██╔══██║██║░░██╗██╔══██║██╔══╝░░  ██╔═══╝░██╔══██╗██║░░██║░██╔██╗░░░╚██╔╝░░
╚█████╔╝██║░░██║╚█████╔╝██║░░██║███████╗  ██║░░░░░██║░░██║╚█████╔╝██╔╝╚██╗░░░██║░░░
░╚════╝░╚═╝░░╚═╝░╚════╝░╚═╝░░╚═╝╚══════╝  ╚═╝░░░░░╚═╝░░╚═╝░╚════╝░╚═╝░░╚═╝░░░╚═╝░░░";

        Console.WriteLine(banner);

        Console.WriteLine("Starting version " + VersionInformation.Version + "...");
    }
}
