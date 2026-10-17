using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;
using SpotlightWin.Infrastructure.Helpers;

namespace SpotlightWin.Infrastructure.Search
{
    public class FileSearchProvider : ISearchProvider
    {
        private const string ConnectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";
        private readonly List<string> _quickSearchFolders = new();

        public Task InitializeAsync()
        {
            try
            {
                AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                AddFolderIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));

                string oneDrive = Environment.GetEnvironmentVariable("OneDrive") ?? string.Empty;
                if (!string.IsNullOrEmpty(oneDrive))
                {
                    AddFolderIfExists(Path.Combine(oneDrive, "Desktop"));
                    AddFolderIfExists(Path.Combine(oneDrive, "Documents"));
                }
            }
            catch { }

            return Task.CompletedTask;
        }

        private void AddFolderIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !_quickSearchFolders.Contains(path))
            {
                _quickSearchFolders.Add(path);
            }
        }

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            return Task.Run<IEnumerable<SearchResult>>(() =>
            {
                var results = new Dictionary<string, SearchResult>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                {
                    return results.Values;
                }

                string cleanQuery = query.Trim();

                // 1. Fast Directory Scan off the UI thread
                foreach (var folder in _quickSearchFolders)
                {
                    try
                    {
                        var matchingFiles = Directory.EnumerateFiles(folder, $"*{cleanQuery}*", new EnumerationOptions
                        {
                            RecurseSubdirectories = true,
                            MaxRecursionDepth = 2,
                            IgnoreInaccessible = true
                        }).Take(8);

                        foreach (var file in matchingFiles)
                        {
                            if (results.ContainsKey(file)) continue;

                            results[file] = new SearchResult
                            {
                                Title = Path.GetFileName(file),
                                Subtitle = $"File • {file}",
                                ExecutionPath = file,
                                IconPath = file,
                                Icon = IconHelper.ExtractAssociatedIcon(file),
                                Score = 60
                            };
                        }
                    }
                    catch { }
                }

                // 2. Windows Search Indexer Query
                try
                {
                    string sqlQuery = $@"
                        SELECT TOP 8 System.ItemNameDisplay, System.ItemPathDisplay, System.ItemType 
                        FROM SystemIndex 
                        WHERE SCOPE = 'file:' 
                          AND System.ItemNameDisplay LIKE '%{cleanQuery.Replace("'", "''")}%' 
                        ORDER BY System.DateModified DESC";

                    using var connection = new OleDbConnection(ConnectionString);
                    connection.Open();

                    using var command = new OleDbCommand(sqlQuery, connection);
                    using var reader = command.ExecuteReader();

                    while (reader != null && reader.Read())
                    {
                        string fileName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                        string filePath = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        string itemType = reader.IsDBNull(2) ? "File" : reader.GetString(2);

                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || results.ContainsKey(filePath)) continue;

                        results[filePath] = new SearchResult
                        {
                            Title = fileName,
                            Subtitle = $"{itemType} • {filePath}",
                            ExecutionPath = filePath,
                            IconPath = filePath,
                            Icon = IconHelper.ExtractAssociatedIcon(filePath),
                            Score = 40
                        };
                    }
                }
                catch { }

                return results.Values;
            });
        }
    }
}