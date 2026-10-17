using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;
using SpotlightWin.Infrastructure.Services;

namespace SpotlightWin.Infrastructure.Search
{
    public class LocalFileSearchProvider : ISearchProvider
    {
        private CancellationTokenSource? _cts;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            string trimmed = query.Trim();

            // Skip AI search commands
            if (trimmed.StartsWith("ai ", StringComparison.OrdinalIgnoreCase) || 
                trimmed.StartsWith("ai:", StringComparison.OrdinalIgnoreCase))
            {
                return results;
            }

            // Parse Drive Inclusions / Exclusions from query
            ParseDriveModifiers(trimmed, out string cleanSearchTerm, out List<string> targetDrives, out List<string> excludedDrives);

            if (string.IsNullOrWhiteSpace(cleanSearchTerm) || cleanSearchTerm.Length < 2)
            {
                return results;
            }

            try
            {
                await Task.Delay(200, token); // Debounce typing speed
            }
            catch (OperationCanceledException) { return results; }

            if (token.IsCancellationRequested) return results;

            // Execute Deep Drive Search
            var foundPaths = await FileSearchService.SearchAllDrivesAsync(cleanSearchTerm, targetDrives, excludedDrives, maxResults: 30, token: token);

            foreach (var path in foundPaths)
            {
                bool isDirectory = Directory.Exists(path);
                string fileName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(fileName)) fileName = path;

                results.Add(new SearchResult
                {
                    Title = isDirectory ? $"📁 {fileName}" : $"📄 {fileName}",
                    Subtitle = path,
                    ExecutionPath = path,
                    Score = 90,
                    IsAiResult = false
                });
            }

            return results;
        }

        private static void ParseDriveModifiers(string query, out string cleanQuery, out List<string> targetDrives, out List<string> excludedDrives)
        {
            targetDrives = new List<string>();
            excludedDrives = new List<string>();
            cleanQuery = query;

            // Match 'c:' or 'd:,e:' at start
            var targetMatch = Regex.Match(query, @"^([a-zA-Z](?:,[a-zA-Z])*):(?:\s+|(?!\\))(.*)", RegexOptions.IgnoreCase);
            if (targetMatch.Success)
            {
                string drivesRaw = targetMatch.Groups[1].Value;
                foreach (var d in drivesRaw.Split(','))
                {
                    targetDrives.Add(d.Trim().ToUpper() + ":");
                }
                cleanQuery = targetMatch.Groups[2].Value.Trim();
                return;
            }

            // Match '!c:' or '-c:' or 'exclude:c'
            var excludeMatch = Regex.Match(query, @"^(?:!|-|exclude:)([a-zA-Z](?:,[a-zA-Z])*):?\s+(.*)", RegexOptions.IgnoreCase);
            if (excludeMatch.Success)
            {
                string drivesRaw = excludeMatch.Groups[1].Value;
                foreach (var d in drivesRaw.Split(','))
                {
                    excludedDrives.Add(d.Trim().ToUpper() + ":");
                }
                cleanQuery = excludeMatch.Groups[2].Value.Trim();
            }
        }
    }
}