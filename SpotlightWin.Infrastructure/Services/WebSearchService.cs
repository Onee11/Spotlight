using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SpotlightWin.Infrastructure.Services
{
    public static class WebSearchService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static WebSearchService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        }

        public static async Task<List<string>> FetchWebResultsAsync(string query, CancellationToken cancellationToken = default)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            // Strategy 1: DuckDuckGo HTML Form POST
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("q", query),
                    new KeyValuePair<string, string>("b", "")
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://html.duckduckgo.com/html/")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string html = await response.Content.ReadAsStringAsync(cancellationToken);
                    var matches = Regex.Matches(html, @"<a class=""result__snippet[^""]*""[^>]*>(.*?)</a>", RegexOptions.Singleline);

                    foreach (Match m in matches)
                    {
                        string snippet = CleanHtml(m.Groups[1].Value);
                        if (!string.IsNullOrWhiteSpace(snippet) && snippet.Length > 15)
                        {
                            results.Add(snippet);
                            if (results.Count >= 5) break;
                        }
                    }
                }
            }
            catch { }

            if (results.Count > 0) return results;

            // Strategy 2: Wikipedia Search API Fallback
            try
            {
                string wikiUrl = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(query)}&utf8=1&format=json";
                string json = await _httpClient.GetStringAsync(wikiUrl, cancellationToken);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("query", out var qObj) &&
                    qObj.TryGetProperty("search", out var searchArr) &&
                    searchArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in searchArr.EnumerateArray())
                    {
                        if (item.TryGetProperty("snippet", out var snipVal))
                        {
                            string snippet = CleanHtml(snipVal.GetString() ?? "");
                            if (!string.IsNullOrWhiteSpace(snippet))
                            {
                                results.Add(snippet);
                                if (results.Count >= 4) break;
                            }
                        }
                    }
                }
            }
            catch { }

            return results;
        }

        public static async Task<string> AskQwenWithWebContextAsync(
            string searchQuery,
            List<string> webSnippets,
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            string context = string.Join("\n- ", webSnippets);
            string prompt = $"User Query: '{searchQuery}'\n\nLive Search Context:\n- {context}\n\n" +
                            "Synthesize a direct, highly accurate, and concise response in markdown.";

            var payload = new
            {
                model = "qwen/qwen3.6-27b",
                messages = new object[]
                {
                    new { role = "system", content = "You are Spotlight AI, powered by Qwen on Groq. Synthesize search results concisely." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            string jsonContent = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                string answer = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "No answer received.";

                return Regex.Replace(answer, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();
            }

            return "⚠️ Unable to parse web context.";
        }

        private static string CleanHtml(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string clean = Regex.Replace(raw, @"<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(clean).Trim();
        }
    }
}