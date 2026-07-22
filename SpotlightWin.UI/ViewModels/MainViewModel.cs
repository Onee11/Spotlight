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
        // --- Native Windows API Imports for System Commands ---
        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;

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

                // 1. Clipboard Copy Engine (for Calculator and AI results)
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
                            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION);
                            break;

                        case "shutdown":
                            Process.Start("shutdown.exe", "/s /t 0");
                            break;

                        case "restart":
                            Process.Start("shutdown.exe", "/r /t 0");
                            break;

                        case "sleep":
                            Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
                            break;
                    }
                    return;
                }

                // 3. Default Execution (Applications, Files, Web URLs)
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