using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

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
            _cts?.Dispose();
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

            string rawPrompt = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : string.Empty;
            
            // Clean trailing speech-to-text punctuation (. ! ? ,)
            string prompt = rawPrompt.Trim().TrimEnd('.', '!', '?', ',');

            // 🪙 1. Instant Coin Flip Detector
            if (prompt.Equals("flip a coin", StringComparison.OrdinalIgnoreCase) || 
                prompt.Equals("coin flip", StringComparison.OrdinalIgnoreCase) ||
                prompt.Equals("flip coin", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResult
                {
                    Title = "🪙 Flipping Coin...",
                    Subtitle = "Press Enter to flip coin",
                    ExecutionPath = "action:coin_flip",
                    Score = 100,
                    IsAiResult = true,
                    FullContent = "COIN_FLIP_UI"
                });
                return results;
            }

            // Clear conversation history
            if (prompt.Equals("clear", StringComparison.OrdinalIgnoreCase) || 
                prompt.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                lock (_historyLock) { _conversationHistory.Clear(); }
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
                    Title = "🤖 Spotlight AI Assistant",
                    Subtitle = "Try: 'search news', 'flip a coin', 'point to File', or 'summarize this article'",
                    ExecutionPath = "copy:Please enter a prompt.",
                    Score = 100,
                    IsAiResult = false
                });
                return results;
            }

            try { await Task.Delay(350, token); }
            catch (OperationCanceledException) { return results; }

            if (token.IsCancellationRequested) return results;

            string apiKey = _settingsService.CurrentSettings?.GeminiApiKey?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                results.Add(new SearchResult
                {
                    Title = "🔑 Setup Groq API Key",
                    Subtitle = "Paste key from console.groq.com into Settings",
                    ExecutionPath = "https://console.groq.com/keys",
                    Score = 100,
                    IsAiResult = false
                });
                return results;
            }

            // 🌐 2. LIVE WEB SEARCH & QWEN SUMMARY
            bool isWebSearch = prompt.StartsWith("search ", StringComparison.OrdinalIgnoreCase) ||
                               prompt.StartsWith("web ", StringComparison.OrdinalIgnoreCase) ||
                               prompt.StartsWith("news ", StringComparison.OrdinalIgnoreCase) ||
                               prompt.StartsWith("latest ", StringComparison.OrdinalIgnoreCase);

            if (isWebSearch)
            {
                string searchQuery = Regex.Replace(prompt, @"^(search|web|news|latest)\s+", "", RegexOptions.IgnoreCase).Trim();

                var webSnippets = await WebSearchService.FetchWebResultsAsync(searchQuery);

                if (token.IsCancellationRequested) return results;

                if (webSnippets.Count > 0)
                {
                    string qwenAnswer = await WebSearchService.AskQwenWithWebContextAsync(searchQuery, webSnippets, apiKey, token);

                    string cleanSummary = Regex.Replace(qwenAnswer, @"\s+", " ").Trim();
                    string singleLineTitle = cleanSummary.Length > 90 ? cleanSummary.Substring(0, 87) + "..." : cleanSummary;

                    results.Add(new SearchResult
                    {
                        Title = $"🌐 Qwen Web Result: {singleLineTitle}",
                        Subtitle = $"{webSnippets.Count} live web sources scraped • Press Enter to copy report",
                        ExecutionPath = $"copy:{qwenAnswer}",
                        Score = 100,
                        IsAiResult = true,
                        FullContent = qwenAnswer
                    });
                }
                else
                {
                    results.Add(new SearchResult
                    {
                        Title = $"🌐 Web Search: '{searchQuery}'",
                        Subtitle = "No live web results were found.",
                        ExecutionPath = "copy:No web search results found.",
                        Score = 100,
                        IsAiResult = false
                    });
                }
                return results;
            }

            // Detect pointing / clicking intent
            bool isPointAction = prompt.StartsWith("point", StringComparison.OrdinalIgnoreCase) ||
                                 prompt.StartsWith("where is", StringComparison.OrdinalIgnoreCase) ||
                                 prompt.StartsWith("where's", StringComparison.OrdinalIgnoreCase) ||
                                 prompt.StartsWith("locate", StringComparison.OrdinalIgnoreCase);

            bool isClickAction = prompt.StartsWith("click", StringComparison.OrdinalIgnoreCase);

            // ⚡ 3. NATIVE WINDOWS UI AUTOMATION (Fast & Exact, Background Thread)
            if (isPointAction || isClickAction)
            {
                var uiMatch = await Task.Run(() => FindNativeUiElement(prompt), token);
                if (token.IsCancellationRequested) return results;

                if (uiMatch.HasValue)
                {
                    var match = uiMatch.Value;
                    string actionType = isClickAction ? "click" : "point";
                    string targetStr = $"action:{actionType}:{match.PercentX},{match.PercentY}|{prompt}";

                    results.Add(new SearchResult
                    {
                        Title = isClickAction ? $"🎯 Native Click: '{match.FoundName}'" : $"🎯 Native Point: '{match.FoundName}'",
                        Subtitle = $"Exact OS Element at {match.PercentX}%, {match.PercentY}%. Press Enter.",
                        ExecutionPath = targetStr,
                        Score = 100,
                        IsAiResult = true,
                        FullContent = $"Element '{match.FoundName}' found with native OS precision at ({match.PercentX}%, {match.PercentY}%)."
                    });
                    return results;
                }
                else
                {
                    results.Add(new SearchResult
                    {
                        Title = $"⚠️ Native Control Not Found",
                        Subtitle = $"Could not locate an OS button or control matching '{prompt}' in active window.",
                        ExecutionPath = "copy:Control not found",
                        Score = 100,
                        IsAiResult = false
                    });
                    return results;
                }
            }

            // 👁️ 4. VISION AI FOR SUMMARIES & TEXT ANALYSIS ONLY
            bool isVisionQuery = DetectVisualIntent(prompt);

            try
            {
                string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
                string modelName = isVisionQuery ? "llama-3.2-11b-vision-preview" : "llama-3.3-70b-versatile";

                var messages = new List<object>();

                if (isVisionQuery)
                {
                    string base64Screen = CapturePrimaryScreenBase64();
                    string systemText = $"You are looking at a screenshot of the user's primary monitor. Answer concisely: {prompt}";

                    messages.Add(new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = systemText },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Screen}" } }
                        }
                    });
                }
                else
                {
                    messages.Add(new
                    {
                        role = "system",
                        content = "You are Spotlight AI desktop assistant. " +
                                  "Support automated system actions. If the user asks to open/do something, reply ONLY on line 1 with:\n" +
                                  "1) DISCORD_MSG:<text_to_send>\n" +
                                  "2) GMAIL:<to_email>|<subject>|<body>\n" +
                                  "3) CMD:<app_name> (e.g. CMD:discord, CMD:notepad, CMD:calc)\n" +
                                  "4) URL:<https_url>\n" +
                                  "Otherwise, give a concise markdown response."
                    });

                    lock (_historyLock) { messages.AddRange(_conversationHistory); }
                    messages.Add(new { role = "user", content = prompt });
                }

                var payload = new { model = modelName, messages = messages, temperature = 0.1 };
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

                    aiAnswer = Regex.Replace(aiAnswer, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();

                    string fullFormattedAnswer = aiAnswer;
                    
                    // Safely truncate title so long summaries don't overflow the UI
                    string cleanText = Regex.Replace(aiAnswer, @"\s+", " ").Trim();
                    string singleLineTitle = cleanText.Length > 90 ? cleanText.Substring(0, 87) + "..." : cleanText;

                    // Parse Advanced Automations
                    if (!isVisionQuery && aiAnswer.StartsWith("DISCORD_MSG:", StringComparison.OrdinalIgnoreCase))
                    {
                        string msg = aiAnswer.Substring(12).Trim();
                        results.Add(new SearchResult
                        {
                            Title = $"💬 Discord Action: Send message '{msg}'",
                            Subtitle = "Press Enter to open Discord & type message automatically",
                            ExecutionPath = $"action:discord_msg:{msg}",
                            Score = 100,
                            IsAiResult = false
                        });
                    }
                    else if (!isVisionQuery && aiAnswer.StartsWith("GMAIL:", StringComparison.OrdinalIgnoreCase))
                    {
                        string emailData = aiAnswer.Substring(6).Trim();
                        results.Add(new SearchResult
                        {
                            Title = $"📧 Gmail Action: Compose email",
                            Subtitle = $"Press Enter to open Gmail directly in browser",
                            ExecutionPath = $"action:gmail:{emailData}",
                            Score = 100,
                            IsAiResult = false
                        });
                    }
                    else if (!isVisionQuery && aiAnswer.StartsWith("CMD:", StringComparison.OrdinalIgnoreCase))
                    {
                        string processToRun = aiAnswer.Substring(4).Trim();
                        results.Add(new SearchResult
                        {
                            Title = $"⚡ App Action: Launch '{processToRun}'",
                            Subtitle = $"Press Enter to run process",
                            ExecutionPath = $"action:cmd:{processToRun}",
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
                                while (_conversationHistory.Count > 8) { _conversationHistory.RemoveAt(0); }
                            }
                        }

                        results.Add(new SearchResult
                        {
                            Title = isVisionQuery ? $"👁️ {singleLineTitle}" : $"🤖 {singleLineTitle}",
                            Subtitle = "Press Enter to copy • Selection shows formatted preview",
                            ExecutionPath = $"copy:{fullFormattedAnswer}",
                            Score = 100,
                            IsAiResult = true,
                            FullContent = fullFormattedAnswer
                        });
                    }
                }
                else
                {
                    string errorMessage = $"API Error ({response.StatusCode})";
                    try
                    {
                        using var errDoc = JsonDocument.Parse(responseBody);
                        if (errDoc.RootElement.TryGetProperty("error", out var errObj) &&
                            errObj.TryGetProperty("message", out var msgVal))
                        {
                            errorMessage = msgVal.GetString() ?? errorMessage;
                        }
                    }
                    catch { }

                    results.Add(new SearchResult
                    {
                        Title = $"⚠️ Groq API Error ({response.StatusCode})",
                        Subtitle = errorMessage,
                        ExecutionPath = $"copy:{errorMessage}",
                        Score = 100,
                        IsAiResult = false
                    });
                }
            }
            catch (OperationCanceledException) { return results; }
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

        private static (double PercentX, double PercentY, string FoundName)? FindNativeUiElement(string targetText)
        {
            if (string.IsNullOrWhiteSpace(targetText)) return null;

            string searchKey = targetText.ToLowerInvariant()
                .Replace("point to", "")
                .Replace("point", "")
                .Replace("click on", "")
                .Replace("click", "")
                .Replace("where is", "")
                .Replace("where's", "")
                .Replace("locate", "")
                .Replace("the", "")
                .Trim();

            if (searchKey.Length < 2) return null;

            try
            {
                IntPtr hwnd = GetForegroundWindow();
                AutomationElement activeWindow = hwnd != IntPtr.Zero 
                    ? AutomationElement.FromHandle(hwnd) 
                    : AutomationElement.RootElement;

                if (activeWindow == null) return null;

                System.Windows.Automation.Condition controlCondition = new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem)
                );

                AutomationElementCollection elements = activeWindow.FindAll(TreeScope.Descendants, controlCondition);
                AutomationElement? bestMatch = null;

                int processedCount = 0;
                const int maxScanLimit = 350; // Prevent high CPU load/freezes on large windows

                foreach (AutomationElement element in elements)
                {
                    if (++processedCount > maxScanLimit) break;

                    try
                    {
                        if (element.Current.IsOffscreen) continue;

                        string name = element.Current.Name;
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        string lowerName = name.ToLowerInvariant();

                        if (lowerName.Equals(searchKey, StringComparison.OrdinalIgnoreCase))
                        {
                            bestMatch = element;
                            break;
                        }

                        if (lowerName.Contains(searchKey) || searchKey.Contains(lowerName))
                        {
                            bestMatch = element;
                        }
                    }
                    catch
                    {
                        // Skip stale or restricted controls
                    }
                }

                if (bestMatch != null)
                {
                    var bounds = bestMatch.Current.BoundingRectangle;
                    if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
                    {
                        var primaryScreen = Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);

                        double centerX = bounds.X + (bounds.Width / 2.0);
                        double centerY = bounds.Y + (bounds.Height / 2.0);

                        double percentX = Math.Round((centerX / primaryScreen.Width) * 100.0, 1);
                        double percentY = Math.Round((centerY / primaryScreen.Height) * 100.0, 1);

                        return (percentX, percentY, bestMatch.Current.Name);
                    }
                }
            }
            catch { }

            return null;
        }

        private static bool DetectVisualIntent(string prompt)
        {
            string p = prompt.ToLowerInvariant();
            if (p.Contains("screen") || p.Contains("display") || p.Contains("monitor") || p.Contains("window")) return true;

            string[] visualKeywords = { "this", "here", "puzzle", "error", "bug", "solve", "explain this", "read this", "summarize this", "fix this", "what is this", "code", "image", "diagram", "chart" };
            foreach (var keyword in visualKeywords)
            {
                if (Regex.IsMatch(p, $@"\b{Regex.Escape(keyword)}\b")) return true;
            }
            return false;
        }

        private static string CapturePrimaryScreenBase64()
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bitmap)) 
            { 
                g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size); 
            }

            int maxWidth = 1280, maxHeight = 720;
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