using System.Collections.Generic;

namespace PostInstallKit;

public static class AppCatalog
{
    public static List<AppItem> GetAll() => new()
    {
        // .NET
        new() { Category = ".NET",  Name = ".NET Desktop Runtime 6",  WingetId = "Microsoft.DotNet.DesktopRuntime.6",  IsSelected = true },
        new() { Category = ".NET",  Name = ".NET Desktop Runtime 8",  WingetId = "Microsoft.DotNet.DesktopRuntime.8",  IsSelected = true },
        new() { Category = ".NET",  Name = ".NET Desktop Runtime 9",  WingetId = "Microsoft.DotNet.DesktopRuntime.9",  IsSelected = true },
        new() { Category = ".NET",  Name = ".NET Desktop Runtime 10", WingetId = "Microsoft.DotNet.DesktopRuntime.10", IsSelected = true },

        // Visual C++ Redistributables
        new() { Category = "VC++",  Name = "VC++ 2005 x86",    WingetId = "Microsoft.VCRedist.2005.x86",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2005 x64",    WingetId = "Microsoft.VCRedist.2005.x64",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2008 x86",    WingetId = "Microsoft.VCRedist.2008.x86",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2008 x64",    WingetId = "Microsoft.VCRedist.2008.x64",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2010 x86",    WingetId = "Microsoft.VCRedist.2010.x86",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2010 x64",    WingetId = "Microsoft.VCRedist.2010.x64",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2012 x86",    WingetId = "Microsoft.VCRedist.2012.x86",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2012 x64",    WingetId = "Microsoft.VCRedist.2012.x64",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2013 x86",    WingetId = "Microsoft.VCRedist.2013.x86",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2013 x64",    WingetId = "Microsoft.VCRedist.2013.x64",    IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2015-2022 x86", WingetId = "Microsoft.VCRedist.2015+.x86", IsSelected = true },
        new() { Category = "VC++",  Name = "VC++ 2015-2022 x64", WingetId = "Microsoft.VCRedist.2015+.x64", IsSelected = true },

        // Java (Eclipse Temurin JDKs). Winget installs the newest update of each version.
        new() { Category = "Java",  Name = "Java 8 JDK (Temurin)",  WingetId = "EclipseAdoptium.Temurin.8.JDK",  Detect = "JDK with Hotspot 8",  IsSelected = true },
        new() { Category = "Java",  Name = "Java 17 JDK (Temurin)", WingetId = "EclipseAdoptium.Temurin.17.JDK", Detect = "JDK with Hotspot 17", IsSelected = true },
        new() { Category = "Java",  Name = "Java 21 JDK (Temurin)", WingetId = "EclipseAdoptium.Temurin.21.JDK", Detect = "JDK with Hotspot 21", IsSelected = true },
        new() { Category = "Java",  Name = "Java 25 JDK (Temurin)", WingetId = "EclipseAdoptium.Temurin.25.JDK", Detect = "JDK with Hotspot 25", IsSelected = true },

        // Other runtimes
        new() { Category = "Runtime", Name = "DirectX End-User Runtime", WingetId = "Microsoft.DirectX", IsSelected = true },

        // Common apps
        new() { Category = "Apps",  Name = "Google Chrome",  WingetId = "Google.Chrome" },
        new() { Category = "Apps",  Name = "Mozilla Firefox", WingetId = "Mozilla.Firefox" },
        new() { Category = "Apps",  Name = "7-Zip",          WingetId = "7zip.7zip" },
        new() { Category = "Apps",  Name = "VLC",            WingetId = "VideoLAN.VLC" },
        new() { Category = "Apps",  Name = "Notepad++",      WingetId = "Notepad++.Notepad++" },
        new() { Category = "Apps",  Name = "VS Code",        WingetId = "Microsoft.VisualStudioCode", Detect = "Visual Studio Code" },
        new() { Category = "Apps",  Name = "Discord",        WingetId = "Discord.Discord" },
        new() { Category = "Apps",  Name = "Steam",          WingetId = "Valve.Steam" },
    };
}