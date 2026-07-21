using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Infrastructure.Input;
using SpotlightWin.Infrastructure.Search;
using SpotlightWin.UI.ViewModels;

namespace SpotlightWin.UI
{
    public partial class App : System.Windows.Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        private ServiceProvider? _serviceProvider;
        private MainWindow? _mainWindow;
        private GlobalHotkeyService? _hotkeyService;
        private NotifyIcon? _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AttachConsole(ATTACH_PARENT_PROCESS);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                Console.WriteLine();
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("[OmniLaunch] Initializing background app...");

                var services = new ServiceCollection();

                services.AddSingleton<GlobalHotkeyService>();

                // Register all search providers
                services.AddSingleton<ISearchProvider, AppSearchProvider>();
                services.AddSingleton<ISearchProvider, CalculatorSearchProvider>();
                services.AddSingleton<ISearchProvider, WebSearchProvider>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();

                _serviceProvider = services.BuildServiceProvider();

                _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                _mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();

                var helper = new WindowInteropHelper(_mainWindow);
                helper.EnsureHandle();

                _hotkeyService = _serviceProvider.GetRequiredService<GlobalHotkeyService>();
                _hotkeyService.Register(helper.Handle);
                _hotkeyService.OnHotkeyPressed += OnHotkeyPressed;

                InitializeTrayIcon();

                Console.WriteLine("[OmniLaunch] App initialized successfully!");
                Console.WriteLine("[OmniLaunch] Press CTRL + SPACE to toggle launcher.");
                Console.WriteLine("--------------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OmniLaunch] FATAL STARTUP ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Shutdown();
            }
        }

        private void InitializeTrayIcon()
        {
            var contextMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("Show OmniLaunch (Ctrl+Space)");
            showItem.Click += (s, e) => ToggleMainWindow();

            var exitItem = new ToolStripMenuItem("Exit Application");
            exitItem.Click += (s, e) => ExitApplication();

            contextMenu.Items.Add(showItem);
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

        private void OnHotkeyPressed()
        {
            ToggleMainWindow();
        }

        private void ToggleMainWindow()
        {
            if (_mainWindow == null) return;

            Dispatcher.Invoke(() =>
            {
                _mainWindow.ToggleWindow();
            });
        }

        private void ExitApplication()
        {
            Console.WriteLine("[OmniLaunch] Exit selected from system tray.");
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            _hotkeyService?.Dispose();
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}