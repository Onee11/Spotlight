using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;
using SpotlightWin.Infrastructure.Helpers;

namespace SpotlightWin.Infrastructure.Search
{
    public class AppSearchProvider : ISearchProvider
    {
        private readonly List<SearchResult> _appCache = new();
        private bool _isInitialized = false;

        public Task InitializeAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                    string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

                    var shortcutPaths = new List<string>();

                    var enumOptions = new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true
                    };

                    if (Directory.Exists(startMenuPath))
                    {
                        shortcutPaths.AddRange(Directory.EnumerateFiles(startMenuPath, "*.lnk", enumOptions));
                    }

                    if (Directory.Exists(commonStartMenuPath))
                    {
                        shortcutPaths.AddRange(Directory.EnumerateFiles(commonStartMenuPath, "*.lnk", enumOptions));
                    }

                    _appCache.Clear();
                    foreach (string path in shortcutPaths.Distinct())
                    {
                        string fileName = Path.GetFileNameWithoutExtension(path);
                        var icon = IconHelper.ExtractAssociatedIcon(path);

                        _appCache.Add(new SearchResult
                        {
                            Title = fileName,
                            Subtitle = "Application",
                            ExecutionPath = path,
                            IconPath = path,
                            Icon = icon
                        });
                    }
                    Console.WriteLine($"[OmniLaunch] Indexed {_appCache.Count} applications with native icons.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OmniLaunch] Indexing error: {ex.Message}");
                }
                finally
                {
                    _isInitialized = true;
                }
            });
        }

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(Enumerable.Empty<SearchResult>());
            }

            var results = _appCache
                .Where(app => app.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(app =>
                {
                    app.Score = app.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 100 : 50;
                    return app;
                })
                .OrderByDescending(app => app.Score)
                .ThenBy(app => app.Title)
                .Take(10)
                .ToList(); // Eagerly evaluate list to prevent thread issues

            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }
    }
}