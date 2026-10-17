using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;

namespace SpotlightWin.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // --- Native Windows API Imports for System Commands & Mouse Control ---
        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI   = 0x00000002;
        private const uint SHERB_NOSOUND        = 0x00000004;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP   = 0x0004;

        private readonly IEnumerable<ISearchProvider> _searchProviders;
        private CancellationTokenSource? _searchCts;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private bool _isVisible = false;

        [ObservableProperty]
        private SearchResult? _selectedResult;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public ObservableCollection<SearchResult> Results { get; } = new();

        public MainViewModel(IEnumerable<ISearchProvider> searchProviders)
        {
            _searchProviders = searchProviders;
            foreach (var provider in _searchProviders)
            {
                _ = provider.InitializeAsync();
            }
        }

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
            if (IsVisible)
            {
                SearchQuery = string.Empty;
                ErrorMessage = string.Empty;
                Results.Clear();
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            _ = PerformSearchAsync(value);
        }

        private async Task PerformSearchAsync(string query)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Results.Clear();
                    SelectedResult = null;
                });
                return;
            }

            try
            {
                // Debounce user input by 150ms to keep search fast and prevent UI lag
                await Task.Delay(150, token);
                if (token.IsCancellationRequested) return;

                var tasks = _searchProviders.Select(p => p.SearchAsync(query));
                var providerResults = await Task.WhenAll(tasks);

                if (token.IsCancellationRequested) return;

                var combinedResults = providerResults
                    .SelectMany(r => r)
                    .OrderByDescending(r => r.Score)
                    .ThenBy(r => r.Title)
                    .Take(15)
                    .ToList();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (token.IsCancellationRequested) return;

                    Results.Clear();
                    foreach (var result in combinedResults)
                    {
                        Results.Add(result);
                    }

                    SelectedResult = Results.FirstOrDefault();
                });
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = $"Search error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExecuteSelected()
        {
            if (SelectedResult == null || string.IsNullOrEmpty(SelectedResult.ExecutionPath)) return;

            try
            {
                string target = SelectedResult.ExecutionPath;

                // 1. Clipboard Copy Engine
                if (target.StartsWith("copy:", StringComparison.OrdinalIgnoreCase))
                {
                    string textToCopy = target.Substring(5);
                    Clipboard.SetText(textToCopy);
                    ToggleVisibility();
                    return;
                }

                // 2. Windows Power Commands Engine
                if (target.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
                {
                    string action = target.Substring(4).ToLowerInvariant();
                    ToggleVisibility();

                    switch (action)
                    {
                        case "lock":
                            LockWorkStation();
                            break;

                        case "recycle":
                            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                            break;

                        case "shutdown":
                            Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0") { UseShellExecute = true });
                            break;

                        case "restart":
                            Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 0") { UseShellExecute = true });
                            break;

                        case "sleep":
                            Process.Start(new ProcessStartInfo("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0") { UseShellExecute = true });
                            break;
                    }
                    return;
                }

                // 3. Spotlight Action Protocol Engine (AI Actions, UI Automation & Custom Commands)
                if (target.StartsWith("action:", StringComparison.OrdinalIgnoreCase))
                {
                    string actionPayload = target.Substring(7);
                    ToggleVisibility();

                    if (actionPayload.Equals("coin_flip", StringComparison.OrdinalIgnoreCase))
                    {
                        string outcome = new Random().Next(0, 2) == 0 ? "🪙 Heads!" : "🪙 Tails!";
                        Clipboard.SetText(outcome);
                        MessageBox.Show(outcome, "Coin Flip Result", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    if (actionPayload.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
                    {
                        string processName = actionPayload.Substring(4).Trim();
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = processName,
                            UseShellExecute = true
                        });
                        return;
                    }

                    if (actionPayload.StartsWith("gmail:", StringComparison.OrdinalIgnoreCase))
                    {
                        string emailData = actionPayload.Substring(6).Trim();
                        string[] parts = emailData.Split('|');
                        string to = parts.Length > 0 ? Uri.EscapeDataString(parts[0]) : "";
                        string subject = parts.Length > 1 ? Uri.EscapeDataString(parts[1]) : "";
                        string body = parts.Length > 2 ? Uri.EscapeDataString(parts[2]) : "";

                        string gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&to={to}&su={subject}&body={body}";
                        Process.Start(new ProcessStartInfo { FileName = gmailUrl, UseShellExecute = true });
                        return;
                    }

                    if (actionPayload.StartsWith("discord_msg:", StringComparison.OrdinalIgnoreCase))
                    {
                        string msg = actionPayload.Substring(12).Trim();
                        Clipboard.SetText(msg);

                        try
                        {
                            Process.Start(new ProcessStartInfo { FileName = "discord://", UseShellExecute = true });
                        }
                        catch
                        {
                            Process.Start(new ProcessStartInfo { FileName = "https://discord.com/app", UseShellExecute = true });
                        }
                        return;
                    }

                    if (actionPayload.StartsWith("point:", StringComparison.OrdinalIgnoreCase) ||
                        actionPayload.StartsWith("click:", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isClick = actionPayload.StartsWith("click:", StringComparison.OrdinalIgnoreCase);
                        string coordsData = actionPayload.Substring(6);
                        string[] parts = coordsData.Split('|');

                        if (parts.Length > 0)
                        {
                            string[] xy = parts[0].Split(',');
                            if (xy.Length == 2 &&
                                double.TryParse(xy[0], out double pctX) &&
                                double.TryParse(xy[1], out double pctY))
                            {
                                var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
                                int posX = (int)(primaryScreen.Width * (pctX / 100.0));
                                int posY = (int)(primaryScreen.Height * (pctY / 100.0));

                                SetCursorPos(posX, posY);

                                if (isClick)
                                {
                                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)posX, (uint)posY, 0, UIntPtr.Zero);
                                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)posX, (uint)posY, 0, UIntPtr.Zero);
                                }
                            }
                        }
                        return;
                    }

                    return;
                }

                // 4. Default Execution (Applications, Files, Web URLs)
                var processInfo = new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                };
                Process.Start(processInfo);
                ToggleVisibility();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to execute '{SelectedResult.Title}': {ex.Message}";
            }
        }
    }
}