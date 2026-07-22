using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using SpotlightWin.Infrastructure.Input;
using SpotlightWin.Infrastructure.Services;

namespace SpotlightWin.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly SettingsService _settingsService;
        private readonly MainWindow _mainWindow;

        private uint _recordedModifiers;
        private uint _recordedKey;
        private double _initialOpacity;

        public SettingsWindow(GlobalHotkeyService hotkeyService, SettingsService settingsService, MainWindow mainWindow)
        {
            InitializeComponent();
            _hotkeyService = hotkeyService;
            _settingsService = settingsService;
            _mainWindow = mainWindow;

            // Ensure Settings stays on top of MainWindow
            Owner = _mainWindow;
            Topmost = true;

            // Show MainWindow for live preview
            _mainWindow.Show();

            // Auto-position Settings window directly below the search launcher
            Left = _mainWindow.Left + (_mainWindow.Width - Width) / 2;
            Top = _mainWindow.Top + 80;

            var current = _settingsService.CurrentSettings;
            _recordedModifiers = current.HotkeyModifiers;
            _recordedKey = current.HotkeyKey;
            _initialOpacity = current.Opacity;
            HotkeyInputBox.Text = current.HotkeyDisplayText;

            OpacitySlider.Value = current.Opacity;
            OpacityValueText.Text = $"{(int)(current.Opacity * 100)}%";

            Closed += SettingsWindow_Closed;
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueText != null)
            {
                OpacityValueText.Text = $"{(int)(e.NewValue * 100)}%";
            }
            if (_mainWindow != null)
            {
                _mainWindow.Opacity = e.NewValue;
            }
        }

        private void HotkeyInputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            uint modifiers = 0;
            var display = new StringBuilder();

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                modifiers |= GlobalHotkeyService.MOD_CONTROL;
                display.Append("Ctrl + ");
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                modifiers |= GlobalHotkeyService.MOD_ALT;
                display.Append("Alt + ");
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                modifiers |= GlobalHotkeyService.MOD_SHIFT;
                display.Append("Shift + ");
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            {
                modifiers |= GlobalHotkeyService.MOD_WIN;
                display.Append("Win + ");
            }

            if (modifiers == 0)
            {
                HotkeyInputBox.Text = "Hold Ctrl, Alt, or Shift + Key!";
                return;
            }

            _recordedModifiers = modifiers;
            _recordedKey = (uint)KeyInterop.VirtualKeyFromKey(key);

            display.Append(key.ToString());
            HotkeyInputBox.Text = display.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(_mainWindow);
            bool success = _hotkeyService.Register(helper.Handle, _recordedModifiers, _recordedKey);

            if (success)
            {
                var updatedSettings = new Core.Models.AppSettings
                {
                    HotkeyModifiers = _recordedModifiers,
                    HotkeyKey = _recordedKey,
                    HotkeyDisplayText = HotkeyInputBox.Text,
                    Opacity = OpacitySlider.Value
                };

                _settingsService.SaveSettings(updatedSettings);
                _initialOpacity = OpacitySlider.Value;
                MessageBox.Show("Settings saved and applied!", "OmniLaunch", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show("Failed to register shortcut. Key combination may be reserved.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsWindow_Closed(object? sender, EventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Opacity = _initialOpacity;
                _mainWindow.Hide();
            }
        }
    }
}