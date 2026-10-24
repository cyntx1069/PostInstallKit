using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace PostInstallKit;

public static class WingetService
{
    // Exit codes that mean "fine": success, "nothing to do", "already installed"
    private static readonly int[] OkCodes =
    {
        0,
        unchecked((int)0x8A15002B),
        unchecked((int)0x8A150061),
    };

    public static bool IsSuccess(int exitCode) => Array.IndexOf(OkCodes, exitCode) >= 0;

    public static async Task<(int ExitCode, string Output)> RunAsync(string arguments, Action<string>? onLine = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "winget",
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
        catch (Win32Exception)
        {
            return (-1, "winget was not found on this PC. Install 'App Installer' from the Microsoft Store.");
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
    }
}