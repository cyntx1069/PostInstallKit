using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace PostInstallKit;

public class ToolItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public static class WindowsTasks
{
    // ---------- Generic process runner ----------

    public static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string arguments, Action<string>? onLine = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        var sb = new StringBuilder();

        try
        {
            using var p = new Process { StartInfo = psi };

            DataReceivedEventHandler handler = (_, e) =>
            {
                if (e.Data == null) return;
                lock (sb) sb.AppendLine(e.Data);
                onLine?.Invoke(e.Data);
            };
            p.OutputDataReceived += handler;
            p.ErrorDataReceived += handler;

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            await p.WaitForExitAsync();

            return (p.ExitCode, sb.ToString());
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
    }

    public static Task<(int ExitCode, string Output)> RunPowerShellAsync(string script, Action<string>? onLine = null)
    {
        // EncodedCommand avoids all quoting problems
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        return RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}", onLine);
    }

    // ---------- Step 1: Windows Update ----------

    public const string WindowsUpdateScript = """
        $ProgressPreference = 'SilentlyContinue'
        $ErrorActionPreference = 'Continue'
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        'Preparing update tools (NuGet + PSWindowsUpdate)...'
        if (-not (Get-PackageProvider -ListAvailable | Where-Object Name -eq 'NuGet')) {
            Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force | Out-Null
        }
        if (-not (Get-Module -ListAvailable -Name PSWindowsUpdate)) {
            Install-Module -Name PSWindowsUpdate -Force -Scope AllUsers -SkipPublisherCheck -AllowClobber
        }
        Import-Module PSWindowsUpdate
        Add-WUServiceManager -MicrosoftUpdate -Confirm:$false | Out-Null
        'Searching for and installing updates (including optional ones). This can take a long time...'
        Install-WindowsUpdate -MicrosoftUpdate -AcceptAll -IgnoreReboot *>&1 | Out-String -Stream
        if (Get-WURebootStatus -Silent) { 'REBOOT_REQUIRED' } else { 'NO_REBOOT_NEEDED' }
        """;

    // ---------- Step 3: Settings ----------

    public static async Task<string> SetHighPerformancePlanAsync()
    {
        const string ultimate = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        const string high = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        // Creates a copy of the hidden Ultimate Performance plan and returns its new GUID
        var (c1, o1) = await RunAsync("powercfg", $"-duplicatescheme {ultimate}");
        var m = Regex.Match(o1, @"[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}");

        if (c1 == 0 && m.Success)
        {
            var (c2, _) = await RunAsync("powercfg", $"-setactive {m.Value}");
            if (c2 == 0) return "Power plan set to Ultimate Performance.";
        }

        var (c3, _) = await RunAsync("powercfg", $"-setactive {high}");
        return c3 == 0
            ? "Ultimate Performance isn't available here, so High Performance was set."
            : "Could not change the power plan (this PC may not allow it).";
    }

    public static async Task NeverSleepOnAcAsync()
    {
        await RunAsync("powercfg", "-change -monitor-timeout-ac 0");
        await RunAsync("powercfg", "-change -standby-timeout-ac 0");
        await RunAsync("powercfg", "-change -hibernate-timeout-ac 0");
        await RunAsync("powercfg", "-change -disk-timeout-ac 0");
    }

    public static void SetUserDword(string subKey, string name, int value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey);
        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    public static void RestartExplorer()
    {
        // Windows starts Explorer again by itself after it is closed
        foreach (var p in Process.GetProcessesByName("explorer"))
        {
            try { p.Kill(); } catch { }
        }
    }
}