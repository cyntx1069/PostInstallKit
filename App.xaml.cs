using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace PostInstallKit
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();

            // Start child programs (Chris Titus tool, PowerShell, winget) from a neutral folder
            // instead of the app folder, which is full of .NET DLLs that can confuse PowerShell.
            Environment.CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();

            // Window and taskbar icon
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
                _window.AppWindow.SetIcon(iconPath);

            _window.Activate();
        }
    }
}