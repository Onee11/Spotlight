using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;
using SpotlightWin.Infrastructure.Services;

namespace SpotlightWin.Infrastructure.Search
{
    public class AiSearchProvider : ISearchProvider
    {
        private readonly SettingsService _settingsService;
        private readonly HttpClient _httpClient = new();
        private CancellationTokenSource? _cts;

        private static readonly List<object> _conversationHistory = new();
        private static readonly object _historyLock = new();

        public AiSearchProvider()
        {
            _settingsService = new SettingsService();
        }

        public AiSearchProvider(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(query)) return results;

            string trimmed = query.Trim();
            if (!trimmed.StartsWith("ai ", StringComparison.OrdinalIgnoreCase) && 
                !trimmed.StartsWith("ai:", StringComparison.OrdinalIgnoreCase))
            {
                return results;
            }

            string prompt = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : string.Empty;

            // Clear conversation history command
            if (prompt.Equals("clear", StringComparison.OrdinalIgnoreCase) || 
                prompt.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                lock (_historyLock)
                {
                    _conversationHistory.Clear();
                }
                results.Add(new SearchResult
                {
                    Title = "🧹 AI Memory Cleared",
                    Subtitle = "Started a fresh conversation session.",
                    ExecutionPath = "copy:Memory cleared",
                    Score = 100,
                    IsAiResult = false
                });
                return results;
            }

            if (string.IsNullOrWhiteSpace(prompt) || prompt.Length < 2)
            {
                results.Add(new SearchResult
                {
                    Title = "🤖 Spotlight AI Assistant (Groq)",
                    Subtitle = "Type 'ai <prompt>' or 'ai look <question>' to analyze your screen!",
                    ExecutionPath = "copy:Please enter a prompt.",
                    Score = 100,
                    IsAiResult = false
                });
                return results;
            }

            try
            {
                // Debounce typing inputs
                await Task.Delay(400, token);
            }
            catch (OperationCanceledException)
            {
                return results;
            }

            if (token.IsCancellationRequested) return results;

            string apiKey = _settingsService.CurrentSettings?.GeminiApiKey?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                results.Add(new SearchResult
                {
                    Title = "🔑 Setup Groq API Key",
                    Subtitle = "Paste your free key from console.groq.com/keys into Settings",
                    ExecutionPath = "https://console.groq.com/keys",
                    Score = 100,
                    IsAiResult = false
                });
                return results;
            }

            // Detect Vision intent ('ai look ...' or 'ai see ...')
            bool isVisionQuery = prompt.StartsWith("look", StringComparison.OrdinalIgnoreCase) || 
                                prompt.StartsWith("see", StringComparison.OrdinalIgnoreCase);

            try
            {
                string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
                
                string modelName = isVisionQuery ? "qwen/qwen3.6-27b" : "llama-3.3-70b-versatile";

                var messages = new List<object>();

                if (isVisionQuery)
                {
                    // Capture primary monitor screen & downscale
                    string base64Screen = CapturePrimaryScreenBase64();

                    string visionPrompt = prompt.Length > 4 
                        ? prompt.Substring(prompt.IndexOf(' ') + 1).Trim() 
                        : "Describe what app or content is active on screen.";

                    messages.Add(new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = $"You are inspecting the user's computer screen. Provide a clear and helpful response: {visionPrompt}" },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Screen}" } }
                        }
                    });
                }
                else
                {
                    messages.Add(new
                    {
                        role = "system",
                        content = "You are Spotlight AI, a Windows assistant. " +
                                  "If the user asks to launch an app, reply ONLY in format CMD:<process_name>. " +
                                  "Otherwise, provide clear, concise answers."
                    });

                    lock (_historyLock)
                    {
                        messages.AddRange(_conversationHistory);
                    }

                    messages.Add(new { role = "user", content = prompt });
                }

                var payload = new
                {
                    model = modelName,
                    messages = messages
                };

                string jsonContent = JsonSerializer.Serialize(payload);

                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, token);
                string responseBody = await response.Content.ReadAsStringAsync(token);

                if (token.IsCancellationRequested) return results;

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    string aiAnswer = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "No response.";

                    // 🧼 Clean up thinking tags from reasoning models (<think>...</think>)
                    aiAnswer = Regex.Replace(aiAnswer, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();
                    
                    string fullFormattedAnswer = aiAnswer;
                    
                    // Create single-line summary for ListBox title
                    string singleLineTitle = Regex.Replace(aiAnswer, @"\s+", " ").Trim();

                    if (!isVisionQuery && aiAnswer.StartsWith("CMD:", StringComparison.OrdinalIgnoreCase))
                    {
                        string processToRun = aiAnswer.Substring(4).Trim();
                        results.Add(new SearchResult
                        {
                            Title = $"⚡ Launch Application: {processToRun}",
                            Subtitle = $"Press Enter to run '{processToRun}'",
                            ExecutionPath = processToRun,
                            Score = 100,
                            IsAiResult = false
                        });
                    }
                    else
                    {
                        if (!isVisionQuery)
                        {
                            lock (_historyLock)
                            {
                                _conversationHistory.Add(new { role = "user", content = prompt });
                                _conversationHistory.Add(new { role = "assistant", content = aiAnswer });

                                while (_conversationHistory.Count > 8)
                                {
                                    _conversationHistory.RemoveAt(0);
                                }
                            }
                        }

                        results.Add(new SearchResult
                        {
                            Title = isVisionQuery ? $"👁️ {singleLineTitle}" : $"🤖 {singleLineTitle}",
                            Subtitle = "Press Enter to copy answer • Selection shows side preview",
                            ExecutionPath = $"copy:{fullFormattedAnswer}",
                            Score = 100,
                            IsAiResult = true,
                            FullContent = fullFormattedAnswer
                        });
                    }
                }
                else
                {
                    results.Add(new SearchResult
                    {
                        Title = "⚠️ AI Error",
                        Subtitle = $"Groq API HTTP {response.StatusCode}.",
                        ExecutionPath = "copy:AI Error",
                        Score = 100,
                        IsAiResult = false
                    });
                }
            }
            catch (OperationCanceledException)
            {
                return results;
            }
            catch (Exception ex)
            {
                results.Add(new SearchResult
                {
                    Title = "⚠️ Request Failed",
                    Subtitle = ex.Message,
                    ExecutionPath = $"copy:{ex.Message}",
                    Score = 100,
                    IsAiResult = false
                });
            }

            return results;
        }

        private static string CapturePrimaryScreenBase64()
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            int maxWidth = 1280;
            int maxHeight = 720;
            double scale = Math.Min((double)maxWidth / bitmap.Width, (double)maxHeight / bitmap.Height);
            if (scale > 1.0) scale = 1.0;

            int newWidth = Math.Max(1, (int)(bitmap.Width * scale));
            int newHeight = Math.Max(1, (int)(bitmap.Height * scale));

            using var resized = new Bitmap(newWidth, newHeight);
            using (var gResized = Graphics.FromImage(resized))
            {
                gResized.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gResized.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }

            using var ms = new MemoryStream();
            resized.Save(ms, ImageFormat.Jpeg);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}