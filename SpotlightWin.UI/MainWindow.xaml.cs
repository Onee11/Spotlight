using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using SpotlightWin.Core.Models;

namespace SpotlightWin.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void ToggleWindow()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
                SearchBox.Focus();
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Esc Key -> Quick Dismiss
            if (e.Key == Key.Escape)
            {
                Hide();
                e.Handled = true;
                return;
            }

            // Ctrl + C -> Copy selected result's file path to Clipboard
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ResultsListBox.SelectedItem is SearchResult selected && !string.IsNullOrEmpty(selected.ExecutionPath))
                {
                    string target = selected.ExecutionPath.StartsWith("copy:") 
                        ? selected.ExecutionPath.Substring(5) 
                        : selected.ExecutionPath;

                    Clipboard.SetText(target);
                    Console.WriteLine($"[OmniLaunch] Copied to clipboard: {target}");
                    e.Handled = true;
                    return;
                }
            }

            // Ctrl + Shift + Enter -> Open containing folder in File Explorer
            if (e.Key == Key.Enter && 
               (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && 
               (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (ResultsListBox.SelectedItem is SearchResult selected && !string.IsNullOrEmpty(selected.ExecutionPath))
                {
                    try
                    {
                        Process.Start("explorer.exe", $"/select,\"{selected.ExecutionPath}\"");
                        Hide();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[OmniLaunch] Failed to reveal in Explorer: {ex.Message}");
                    }
                    e.Handled = true;
                    return;
                }
            }

            // Arrow Key Navigation
            if (ResultsListBox.Items.Count == 0) return;

            if (e.Key == Key.Down)
            {
                if (ResultsListBox.SelectedIndex < ResultsListBox.Items.Count - 1)
                {
                    ResultsListBox.SelectedIndex++;
                    ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (ResultsListBox.SelectedIndex > 0)
                {
                    ResultsListBox.SelectedIndex--;
                    ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
                }
                e.Handled = true;
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            // Hide window when clicking away, unless Settings window is active
            if (!IsSettingsWindowOpen())
            {
                Hide();
            }
        }

        private bool IsSettingsWindowOpen()
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is SettingsWindow && win.IsVisible) return true;
            }
            return false;
        }
    }
}