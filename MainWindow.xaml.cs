using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PostInstallKit;

public sealed partial class MainWindow : Window
{
    private const string TitusCommand = "irm 'https://christitus.com/win' | iex";

    // File types the Activate tab will list and launch
    private static readonly string[] ToolExtensions = { ".cmd", ".bat", ".exe" };

    public List<AppItem> AllApps { get; } = AppCatalog.GetAll();
    public ObservableCollection<AppItem> RuntimeApps { get; }
    public ObservableCollection<AppItem> CommonApps { get; }
    public ObservableCollection<string> Profiles { get; } = new();
    public ObservableCollection<ToolItem> ActivationTools { get; } = new();

    private List<AppItem> _failed = new();
    private bool _busy;

    public MainWindow()
    {
        RuntimeApps = new(AllApps.Where(a => a.Category != "Apps"));
        CommonApps = new(AllApps.Where(a => a.Category == "Apps"));

        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 900));

        // Custom title bar that blends into the dark background
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = Colors.White;
        titleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 110, 120, 135);
        titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(40, 255, 255, 255);
        titleBar.ButtonHoverForegroundColor = Colors.White;

        // Show the first tab
        NavView.SelectedItem = NavView.MenuItems[0];

        RefreshProfiles();
        LoadTools();
        UpdateTitusStatus();
        Log("Ready. Scanning installed apps...");
        _ = ScanInstalledAsync();
    }

    // ---------- Tabs ----------

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string tag) return;

        foreach (var page in PagesHost.Children.OfType<FrameworkElement>())
            page.Visibility = (page.Tag as string) == tag ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------- Selection ----------

    private ObservableCollection<AppItem> GroupFor(object sender)
        => (sender as FrameworkElement)?.Tag as string == "runtimes" ? RuntimeApps : CommonApps;

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var a in GroupFor(sender)) a.IsSelected = true;
    }

    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var a in GroupFor(sender)) a.IsSelected = false;
    }

    // ---------- Step 1: Windows Update ----------

    private async void WindowsUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        SetBusy(true, indeterminate: true);
        Log("Starting Windows Update. This can take a long time, so be patient...");

        var (code, output) = await WindowsTasks.RunPowerShellAsync(WindowsTasks.WindowsUpdateScript, LogFromProcess);

        if (output.Contains("REBOOT_REQUIRED"))
            Log("Updates installed and a restart is required. Press 'Restart PC', then run Windows Update again until nothing new is found.");
        else if (code == 0)
            Log("Windows Update finished. No restart needed. Run it once more to check for new updates.");
        else
            Log($"Windows Update ended with exit code {code}. Check the log above.");

        SetBusy(false);
    }

    private void OpenWuSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log("Could not open Settings: " + ex.Message);
        }
    }

    private async void Restart_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Restart PC?",
            Content = "Windows will restart in 5 seconds. Save anything you have open first.",
            PrimaryButtonText = "Restart",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = ElementTheme.Dark,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        Process.Start(new ProcessStartInfo("shutdown", "/r /t 5") { UseShellExecute = false, CreateNoWindow = true });
    }

    // ---------- Step 3: Settings ----------

    private async void ApplyTweaks_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        SetBusy(true, indeterminate: true);

        const string advanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        const string desktopIcons = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
        bool explorerChanged = false;

        try
        {
            if (PowerPlanCheck.IsChecked == true)
                Log(await WindowsTasks.SetHighPerformancePlanAsync());

            if (NeverSleepCheck.IsChecked == true)
            {
                await WindowsTasks.NeverSleepOnAcAsync();
                Log("Screen, sleep and hibernate timeouts (plugged in) set to never.");
            }

            if (FileExtCheck.IsChecked == true)
            {
                WindowsTasks.SetUserDword(advanced, "HideFileExt", 0);
                Log("File extensions are now shown.");
                explorerChanged = true;
            }

            if (HiddenFilesCheck.IsChecked == true)
            {
                WindowsTasks.SetUserDword(advanced, "Hidden", 1);
                Log("Hidden files and folders are now shown.");
                explorerChanged = true;
            }

            if (ThisPcCheck.IsChecked == true)
            {
                WindowsTasks.SetUserDword(desktopIcons, "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                Log("'This PC' icon enabled on the desktop.");
                explorerChanged = true;
            }

            if (NetFx3Check.IsChecked == true)
            {
                Log("Enabling .NET Framework 3.5 (this can take a few minutes)...");
                var (code, _) = await WindowsTasks.RunAsync("dism.exe",
                    "/Online /Enable-Feature /FeatureName:NetFx3 /All /NoRestart", LogFromProcess);
                Log(code == 0 || code == 3010
                    ? ".NET Framework 3.5 enabled."
                    : $".NET Framework 3.5 failed (exit code {code}). It needs an internet connection.");
            }

            if (explorerChanged)
            {
                WindowsTasks.RestartExplorer();
                Log("Restarted Explorer so the changes show up (the screen may flicker).");
            }

            Log("Settings applied.");
        }
        catch (Exception ex)
        {
            Log("Error while applying settings: " + ex.Message);
        }

        SetBusy(false);
    }

    // ---------- Step 4: Chris Titus tool ----------

    private static string TitusConfigPath => Path.Combine(ToolsRoot, "Titus", "config.json");

    private void UpdateTitusStatus()
    {
        TitusConfigStatus.Text = File.Exists(TitusConfigPath)
            ? @"Config found: Tools\Titus\config.json"
            : @"No config yet. Export one from the tool (gear icon, top right, Export) and save it as Tools\Titus\config.json.";
    }

    private void LaunchTitus(string script, string logMessage)
    {
        try
        {
            // EncodedCommand avoids quoting problems with the paths and quotes in the script
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            Process.Start(new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}")
            {
                UseShellExecute = true,
            });
            Log(logMessage);
        }
        catch (Exception ex)
        {
            Log("Could not launch the tool: " + ex.Message);
        }
    }

    private void LaunchTitusWithConfig(bool run)
    {
        UpdateTitusStatus();

        if (!File.Exists(TitusConfigPath))
        {
            Log(@"No config found at Tools\Titus\config.json. Export one from the tool first.");
            return;
        }

        var path = TitusConfigPath.Replace("'", "''");
        var script = $"& ([ScriptBlock]::Create((irm 'https://christitus.com/win'))) -Config '{path}'"
                     + (run ? " -Run" : "");

        LaunchTitus(script, run
            ? "Launched the Chris Titus tool and started running your config automatically."
            : "Launched the Chris Titus tool with your config loaded. Review it, then click Run in the tool.");
    }

    private void Titus_Click(object sender, RoutedEventArgs e)
        => LaunchTitus(TitusCommand, "Launched the Chris Titus tool in a new window (it needs internet).");

    private void TitusConfig_Click(object sender, RoutedEventArgs e) => LaunchTitusWithConfig(run: false);

    private void TitusConfigRun_Click(object sender, RoutedEventArgs e) => LaunchTitusWithConfig(run: true);

    private void OpenTitusFolder_Click(object sender, RoutedEventArgs e)
    {
        var dir = Path.Combine(ToolsRoot, "Titus");
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        UpdateTitusStatus();
    }

    // ---------- Step 5: Activation tool launcher ----------

    private static string ToolsRoot => Path.Combine(AppContext.BaseDirectory, "Tools");

    private void LoadTools()
    {
        ActivationTools.Clear();
        var dir = Path.Combine(ToolsRoot, "Activation");
        Directory.CreateDirectory(dir);

        var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
            .Where(f => ToolExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f);

        foreach (var file in files)
            ActivationTools.Add(new ToolItem { Name = Path.GetFileName(file), Path = file });
    }

    private void RefreshTools_Click(object sender, RoutedEventArgs e)
    {
        LoadTools();
        Log($"Tools refreshed: {ActivationTools.Count} found.");
    }

    private void OpenToolsFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(ToolsRoot);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{ToolsRoot}\"") { UseShellExecute = true });
    }

    private void LaunchTool_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string path) return;

        if (!File.Exists(path))
        {
            Log("Tool not found: " + path);
            return;
        }

        try
        {
            // Works for .cmd, .bat and .exe. The file's own folder is the working
            // directory, so any helper files next to it are found.
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path)!,
            });
            Log("Launched " + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            Log("Could not launch the tool: " + ex.Message);
        }
    }

    // ---------- Step 7: Driver website links ----------

    private void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string url) return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            Log("Opened " + url);
        }
        catch (Exception ex)
        {
            Log("Could not open the link: " + ex.Message);
        }
    }

    // ---------- Installed detection ----------

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        await ScanInstalledAsync();
    }

    private async Task ScanInstalledAsync()
    {
        if (_busy) return;
        SetBusy(true, indeterminate: true, showLog: false);

        // Source 1: winget's own list
        var (code, output) = await WingetService.RunAsync("list --accept-source-agreements");
        bool wingetOk = output.Length > 0 && !output.Contains("winget was not found");
        if (!wingetOk)
            Log("winget scan failed (" + output.Trim() + "). Using the Windows program list only.");

        // Source 2: Windows' "Apps & features" list (catches installs made outside winget)
        var programNames = await Task.Run(InstalledPrograms.GetDisplayNames);

        int found = 0;
        foreach (var app in AllApps)
        {
            string keyword = string.IsNullOrWhiteSpace(app.Detect) ? app.Name : app.Detect;

            bool inWinget = wingetOk && output.Contains(app.WingetId, StringComparison.OrdinalIgnoreCase);
            bool inWindows = programNames.Any(n => n.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            bool installed = inWinget || inWindows;
            app.IsInstalled = installed;
            app.Status = installed ? "Installed" : "";
            if (installed) found++;
        }

        Log($"Scan finished: {found} of {AllApps.Count} already installed.");
        SetBusy(false);
    }

    // ---------- Installing ----------

    private async void InstallGroup_Click(object sender, RoutedEventArgs e)
    {
        bool skip = SkipInstalledCheck.IsChecked == true;
        var items = GroupFor(sender).Where(a => a.IsSelected && !(skip && a.IsInstalled)).ToList();
        await InstallAsync(items);
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await InstallAsync(_failed.ToList());
    }

    private async Task InstallAsync(List<AppItem> items)
    {
        if (_busy) return;

        if (items.Count == 0)
        {
            Log("Nothing to install.");
            return;
        }

        SetBusy(true);
        _failed = new List<AppItem>();
        Progress.Maximum = items.Count;
        Progress.Value = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            item.Status = "Installing...";
            Log($"[{i + 1}/{items.Count}] Installing {item.Name} ...");

            var args = $"install --id {item.WingetId} -e --silent --accept-package-agreements --accept-source-agreements";
            var (code, output) = await WingetService.RunAsync(args, LogFromProcess);

            if (WingetService.IsSuccess(code))
            {
                item.IsInstalled = true;
                item.Status = "Installed";
                Log($"    Done: {item.Name}");
            }
            else
            {
                item.Status = "Failed";
                _failed.Add(item);
                Log($"    FAILED: {item.Name} (exit code {code})");
            }

            Progress.Value = i + 1;
        }

        Log(_failed.Count == 0
            ? "All finished with no errors."
            : $"Finished. {_failed.Count} failed. Press 'Retry failed' to try them again.");

        SetBusy(false);
    }

    private void LogFromProcess(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        var text = line.Trim();
        // Skip spinner / progress-bar lines and PowerShell's internal progress XML
        if (text.Length <= 2 || text.Contains('█') || text.Contains('▒')) return;
        if (text.StartsWith("#< CLIXML") || text.StartsWith("<Objs")) return;
        DispatcherQueue.TryEnqueue(() => Log("    " + text));
    }

    private void SetBusy(bool busy, bool indeterminate = false, bool showLog = true)
    {
        _busy = busy;

        // Panels have no IsEnabled in WinUI 3, so disable the controls inside the top bar
        SetEnabledRecursive(TopBar, !busy);
        NavView.IsEnabled = !busy;
        RetryButton.IsEnabled = !busy && _failed.Count > 0;
        Progress.IsIndeterminate = busy && indeterminate;

        // Open the log automatically when a real task starts
        if (busy && showLog) LogExpander.IsExpanded = true;
    }

    private static void SetEnabledRecursive(UIElement element, bool enabled)
    {
        if (element is Control control)
        {
            control.IsEnabled = enabled;
        }
        else if (element is Panel panel)
        {
            foreach (var child in panel.Children)
                SetEnabledRecursive(child, enabled);
        }
    }

    // ---------- Profiles ----------

    private void RefreshProfiles(string? select = null)
    {
        Profiles.Clear();
        foreach (var name in ProfileManager.GetProfileNames())
            Profiles.Add(name);

        if (select != null && Profiles.Contains(select))
            ProfileCombo.SelectedItem = select;
    }

    private void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is not string name)
        {
            Log("Pick a profile first.");
            return;
        }

        var ids = ProfileManager.Load(name);
        foreach (var app in AllApps)
            app.IsSelected = ids.Contains(app.WingetId);

        Log($"Loaded profile '{name}' ({ids.Count} items).");
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        var box = new TextBox
        {
            PlaceholderText = "Profile name",
            Text = ProfileCombo.SelectedItem as string ?? "",
        };

        var dialog = new ContentDialog
        {
            Title = "Save profile",
            Content = box,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = ElementTheme.Dark,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var name = box.Text.Trim();
        if (name.Length == 0)
        {
            Log("Profile name can't be empty.");
            return;
        }

        var ids = AllApps.Where(a => a.IsSelected).Select(a => a.WingetId).ToList();
        ProfileManager.Save(name, ids);
        RefreshProfiles(name);
        Log($"Saved profile '{name}' ({ids.Count} items).");
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is not string name)
        {
            Log("Pick a profile first.");
            return;
        }

        ProfileManager.Delete(name);
        RefreshProfiles();
        Log($"Deleted profile '{name}'.");
    }

    // ---------- Log ----------

    private void Log(string message)
    {
        LogText.Text += message + Environment.NewLine;
        LogLastLine.Text = message.Trim();
        LogScroll.UpdateLayout();
        LogScroll.ChangeView(null, LogScroll.ScrollableHeight, null);
    }
}