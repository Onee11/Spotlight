using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using SpotlightWin.Core.Models;
using SpotlightWin.Infrastructure.Services;

namespace SpotlightWin.UI
{
    public partial class MainWindow : Window
    {
        private readonly VoiceService _voiceService = new();
        private readonly SettingsService _settingsService = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            // 🎯 Immediate Keyboard Focus on window open
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                SearchBox.Focus();
                Keyboard.Focus(SearchBox);
            }));
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
                Keyboard.Focus(SearchBox);
            }
        }

        private async void ToggleVoiceRecording()
        {
            string apiKey = _settingsService.CurrentSettings?.GeminiApiKey?.Trim() ?? "";
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please configure your Groq API key in Settings first!", "Groq Key Required");
                return;
            }

            if (!_voiceService.IsRecording)
            {
                _voiceService.StartRecording();
                MicButton.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Pulse Red
                MicStatusIcon.Text = "🔴";
            }
            else
            {
                MicButton.Background = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255));
                MicStatusIcon.Text = "⏳";

                string text = await _voiceService.StopRecordingAndTranscribeAsync(apiKey);
                MicStatusIcon.Text = "🎙️";

                if (!string.IsNullOrWhiteSpace(text))
                {
                    // 🔧 FIX: Clean trailing punctuation (. ! ? ,) from speech engine
                    text = text.Trim().TrimEnd('.', '!', '?', ',');

                    SearchBox.Text = $"ai {text}";
                    SearchBox.CaretIndex = SearchBox.Text.Length;
                }
            }
        }

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleVoiceRecording();
        }

        private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is SearchResult selected && selected.IsAiResult)
            {
                if (selected.FullContent == "COIN_FLIP_UI")
                {
                    TriggerCoinFlipAnimation();
                }
                else if (!string.IsNullOrWhiteSpace(selected.FullContent))
                {
                    RenderMarkdownInRichTextBox(selected.FullContent);
                }
                AiPreviewPane.Visibility = Visibility.Visible;
            }
            else
            {
                if (AiPreviewPane != null) AiPreviewPane.Visibility = Visibility.Collapsed;
            }
        }

        private async void TriggerCoinFlipAnimation()
        {
            if (AiPreviewBox == null) return;

            // 1. Prepare Container Canvas
            var canvas = new Canvas
            {
                Width = 260,
                Height = 240,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            // 2. Decide outcome (Heads or Tails)
            bool isHeads = new Random().Next(0, 2) == 0;
            string outcomeText = isHeads ? "HEADS" : "TAILS";
            Color coinPrimaryColor = isHeads ? Color.FromRgb(255, 215, 0) : Color.FromRgb(210, 210, 220); // Gold vs Silver
            Color coinDarkColor = isHeads ? Color.FromRgb(160, 110, 10) : Color.FromRgb(100, 100, 110);

            // 3. Create the 3D Vector Coin Graphic
            var coinBorder = new Border
            {
                Width = 110,
                Height = 110,
                CornerRadius = new CornerRadius(55),
                Background = new LinearGradientBrush(
                    coinPrimaryColor,
                    coinDarkColor,
                    45),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(3),
                Effect = new DropShadowEffect
                {
                    Color = coinPrimaryColor,
                    BlurRadius = 25,
                    ShadowDepth = 0,
                    Opacity = 0.85
                },
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            var transformGroup = new TransformGroup();
            var scaleTransform = new ScaleTransform(1, 1);
            transformGroup.Children.Add(scaleTransform);
            coinBorder.RenderTransform = transformGroup;

            // Coin Text
            var coinText = new TextBlock
            {
                Text = isHeads ? "🪙\nHEADS" : "📀\nTAILS",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            coinBorder.Child = coinText;

            Canvas.SetLeft(coinBorder, 75);
            Canvas.SetTop(coinBorder, 30);
            canvas.Children.Add(coinBorder);

            // Status Result Text below
            var resultLabel = new TextBlock
            {
                Text = "Flipping...",
                FontSize = 20,
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.Gray,
                Width = 260,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetTop(resultLabel, 175);
            canvas.Children.Add(resultLabel);

            // Embed canvas inside RichTextBox
            AiPreviewBox.Document.Blocks.Clear();
            AiPreviewBox.Document.Blocks.Add(new BlockUIContainer(canvas));

            // 4. Build Cinematic 1.8-Second 3D Flip Physics Animation
            var storyboard = new Storyboard();

            // 3D Flip Illusion
            var flipAnim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(1800) };
            double currentScale = 1.0;
            for (int i = 0; i < 12; i++)
            {
                currentScale *= -1;
                double keyTimeMs = 1800 * Math.Pow((i + 1) / 12.0, 0.65);
                flipAnim.KeyFrames.Add(new LinearDoubleKeyFrame(currentScale, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(keyTimeMs))));
            }
            flipAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1800))));

            Storyboard.SetTarget(flipAnim, coinBorder);
            Storyboard.SetTargetProperty(flipAnim, new PropertyPath("RenderTransform.Children[0].ScaleX"));
            storyboard.Children.Add(flipAnim);

            // Vertical Jump Arc
            var jumpAnim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(1800) };
            jumpAnim.KeyFrames.Add(new LinearDoubleKeyFrame(30, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            jumpAnim.KeyFrames.Add(new SplineDoubleKeyFrame(-25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(700))));
            jumpAnim.KeyFrames.Add(new EasingDoubleKeyFrame(30, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1800)))
            {
                EasingFunction = new BounceEase { Bounces = 2, Bounciness = 2.5, EasingMode = EasingMode.EaseOut }
            });

            Storyboard.SetTarget(jumpAnim, coinBorder);
            Storyboard.SetTargetProperty(jumpAnim, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(jumpAnim);

            // Start Animation
            storyboard.Begin();

            await Task.Delay(1800);

            resultLabel.Text = $"✨ {outcomeText}! ✨";
            resultLabel.Foreground = new SolidColorBrush(coinPrimaryColor);
        }

        private void RenderMarkdownInRichTextBox(string markdown)
        {
            AiPreviewBox.Document.Blocks.Clear();
            var doc = new FlowDocument();
            var paragraph = new Paragraph { LineHeight = 20 };

            string[] tokens = Regex.Split(markdown, @"(\*\*.*?\*\*|`.*?`|\n)");

            foreach (var token in tokens)
            {
                if (token == "\n")
                {
                    doc.Blocks.Add(paragraph);
                    paragraph = new Paragraph { LineHeight = 20 };
                }
                else if (token.StartsWith("**") && token.EndsWith("**") && token.Length > 4)
                {
                    paragraph.Inlines.Add(new Run(token.Substring(2, token.Length - 4)) { FontWeight = FontWeights.Bold, Foreground = Brushes.White });
                }
                else if (token.StartsWith("`") && token.EndsWith("`") && token.Length > 2)
                {
                    paragraph.Inlines.Add(new Run(token.Substring(1, token.Length - 2))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 255))
                    });
                }
                else
                {
                    paragraph.Inlines.Add(new Run(token));
                }
            }

            doc.Blocks.Add(paragraph);
            AiPreviewBox.Document = doc;
        }

        private async void ExecuteSelectedItem()
        {
            if (ResultsListBox.SelectedItem is SearchResult selected && !string.IsNullOrEmpty(selected.ExecutionPath))
            {
                string target = selected.ExecutionPath;

                // 🎯 Screen Pointing & Mouse Clicking Action
                if (target.StartsWith("action:point:") || target.StartsWith("action:click:"))
                {
                    bool isClick = target.StartsWith("action:click:");
                    string data = target.Substring(isClick ? 13 : 13);
                    string[] parts = data.Split('|');

                    if (parts.Length > 0)
                    {
                        string[] coords = parts[0].Split(',');
                        if (coords.Length == 2 &&
                            double.TryParse(coords[0], out double pctX) &&
                            double.TryParse(coords[1], out double pctY))
                        {
                            double targetX = SystemParameters.PrimaryScreenWidth * (pctX / 100.0);
                            double targetY = SystemParameters.PrimaryScreenHeight * (pctY / 100.0);
                            string label = parts.Length > 1 ? parts[1] : "Target";

                            Hide(); // Hide main launcher window

                            // Render visual screen pointer overlay
                            ScreenPointerWindow.ShowAt(targetX, targetY, label);

                            if (isClick)
                            {
                                await Task.Delay(400);
                                await MouseSimulator.ClickAtAsync(targetX, targetY);
                            }
                            return;
                        }
                    }
                }
                else if (target.StartsWith("copy:"))
                {
                    Clipboard.SetText(target.Substring(5));
                    Hide();
                }
                else if (target.StartsWith("action:discord_msg:"))
                {
                    string msg = target.Substring(19);
                    Hide();
                    await WindowsAutomationEngine.SendDiscordMessageAsync(msg);
                }
                else if (target.StartsWith("action:gmail:"))
                {
                    string[] parts = target.Substring(13).Split('|');
                    string to = parts.Length > 0 ? parts[0] : "";
                    string subject = parts.Length > 1 ? parts[1] : "";
                    string body = parts.Length > 2 ? parts[2] : "";
                    WindowsAutomationEngine.OpenGmailCompose(to, subject, body);
                    Hide();
                }
                else if (target.StartsWith("action:cmd:"))
                {
                    string app = target.Substring(11).ToLowerInvariant();
                    if (app.Contains("discord"))
                    {
                        WindowsAutomationEngine.LaunchDiscord();
                    }
                    else
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo { FileName = app, UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Could not launch {app}: {ex.Message}");
                        }
                    }
                    Hide();
                }
                else if (target == "action:coin_flip")
                {
                    TriggerCoinFlipAnimation();
                }
                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = target,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not execute action: {ex.Message}");
                    }
                    Hide();
                }
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 🎙️ Voice Hotkey: Ctrl + M or F4
            if ((e.Key == Key.M && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) || e.Key == Key.F4)
            {
                ToggleVoiceRecording();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                Hide();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                ExecuteSelectedItem();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ResultsListBox.SelectedItem is SearchResult selected && !string.IsNullOrEmpty(selected.ExecutionPath))
                {
                    string target = selected.ExecutionPath.StartsWith("copy:") 
                        ? selected.ExecutionPath.Substring(5) 
                        : selected.ExecutionPath;

                    Clipboard.SetText(target);
                    e.Handled = true;
                    return;
                }
            }

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
            if (!IsSettingsWindowOpen()) Hide();
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