using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Infrastructure.Input;
using SpotlightWin.Infrastructure.Search;
using SpotlightWin.Infrastructure.Services;
using SpotlightWin.UI.ViewModels;

namespace SpotlightWin.UI
{
    public partial class App : System.Windows.Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        private IServiceProvider? _serviceProvider;
        private MainWindow? _mainWindow;
        private NotifyIcon? _notifyIcon;
        private GlobalHotkeyService? _hotkeyService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Attach terminal output for real-time console logging
            AttachConsole(ATTACH_PARENT_PROCESS);

            // 2. Prevent WPF from closing when the search window hides
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("[OmniLaunch] Initializing background app...");

            var services = new ServiceCollection();

            // Services & Settings
            var settingsService = new SettingsService();
            services.AddSingleton(settingsService);

            // Register Search Providers
            services.AddSingleton<ISearchProvider, AppSearchProvider>();
            services.AddSingleton<ISearchProvider, CalculatorSearchProvider>();
            services.AddSingleton<ISearchProvider, WebSearchProvider>();
            services.AddSingleton<ISearchProvider, FileSearchProvider>();

            // Register ViewModel, Window, and Hotkey Service
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<GlobalHotkeyService>();

            _serviceProvider = services.BuildServiceProvider();

            // 3. Connect ViewModel to MainWindow DataContext (Fixes broken search)
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            _mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();

            // Ensure HWND Handle exists for global hotkey hook
            var helper = new WindowInteropHelper(_mainWindow);
            helper.EnsureHandle();

            // Register Global Hotkey & Apply Opacity Settings
            _hotkeyService = _serviceProvider.GetRequiredService<GlobalHotkeyService>();
            var config = settingsService.CurrentSettings;
            _hotkeyService.Register(helper.Handle, config.HotkeyModifiers, config.HotkeyKey);
            _hotkeyService.OnHotkeyPressed += ToggleMainWindow;

            _mainWindow.Opacity = config.Opacity;

            // Setup Tray Icon
            InitializeTrayIcon();

            Console.WriteLine("[OmniLaunch] App initialized successfully!");
            Console.WriteLine($"[OmniLaunch] Active Hotkey: {config.HotkeyDisplayText}");
            Console.WriteLine("--------------------------------------------------");
        }

        private void ToggleMainWindow()
        {
            if (_mainWindow == null) return;

            Dispatcher.Invoke(() =>
            {
                if (_mainWindow.IsVisible)
                {
                    _mainWindow.Hide();
                }
                else
                {
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    _mainWindow.Focus();
                }
            });
        }

        private void InitializeTrayIcon()
        {
            var contextMenu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Show OmniLaunch");
            showItem.Click += (s, e) => ToggleMainWindow();

            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (s, e) => OpenSettingsWindow();

            var autoStartItem = new ToolStripMenuItem("Start with Windows")
            {
                Checked = IsAutoStartEnabled()
            };
            autoStartItem.Click += (s, e) =>
            {
                bool enable = !autoStartItem.Checked;
                SetAutoStart(enable);
                autoStartItem.Checked = enable;
            };

            var exitItem = new ToolStripMenuItem("Exit Application");
            exitItem.Click += (s, e) => ExitApplication();

            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(autoStartItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "OmniLaunch",
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ToggleMainWindow();
        }

        private void OpenSettingsWindow()
        {
            if (_mainWindow == null || _hotkeyService == null || _serviceProvider == null) return;

            Dispatcher.Invoke(() =>
            {
                var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
                var settingsWin = new SettingsWindow(_hotkeyService, settingsService, _mainWindow);
                settingsWin.ShowDialog();
            });
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("OmniLaunch") != null;
            }
            catch
            {
                return false;
            }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue("OmniLaunch", $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue("OmniLaunch", false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to update startup registry: {ex.Message}");
            }
        }

        private void ExitApplication()
        {
            _hotkeyService?.Dispose();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            Shutdown();
        }
    }
}