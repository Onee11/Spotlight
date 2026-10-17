using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Infrastructure.Search
{
    public class WebSearchProvider : ISearchProvider
    {
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            string trimmed = query.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return Task.FromResult<IEnumerable<SearchResult>>(results);
            }

            // 1. Direct URL Trigger
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                string url = trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? $"https://{trimmed}" : trimmed;
                results.Add(new SearchResult
                {
                    Title = $"Open {trimmed}",
                    Subtitle = "Web Link - Open in default browser",
                    ExecutionPath = url,
                    Score = 950
                });
                return Task.FromResult<IEnumerable<SearchResult>>(results);
            }

            // 2. Google Search Trigger ("g <query>" or "google <query>")
            if (trimmed.StartsWith("g ", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("google ", StringComparison.OrdinalIgnoreCase))
            {
                string searchTerm = ExtractTerm(trimmed, trimmed.StartsWith("g ", StringComparison.OrdinalIgnoreCase) ? 2 : 7);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    results.Add(new SearchResult
                    {
                        Title = $"Search Google for \"{searchTerm}\"",
                        Subtitle = "Web Search",
                        ExecutionPath = $"https://www.google.com/search?q={Uri.EscapeDataString(searchTerm)}",
                        Score = 950
                    });
                }
            }

            // 3. YouTube Search Trigger ("yt <query>" or "youtube <query>")
            if (trimmed.StartsWith("yt ", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("youtube ", StringComparison.OrdinalIgnoreCase))
            {
                string searchTerm = ExtractTerm(trimmed, trimmed.StartsWith("yt ", StringComparison.OrdinalIgnoreCase) ? 3 : 8);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    results.Add(new SearchResult
                    {
                        Title = $"Search YouTube for \"{searchTerm}\"",
                        Subtitle = "Web Search",
                        ExecutionPath = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(searchTerm)}",
                        Score = 950
                    });
                }
            }

            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }

        private static string ExtractTerm(string query, int prefixLength)
        {
            return query.Length > prefixLength ? query.Substring(prefixLength).Trim() : string.Empty;
        }
    }
}