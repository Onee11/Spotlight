using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Infrastructure.Search
{
    public class EverythingSearchProvider : ISearchProvider
    {
        private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const int EVERYTHING_REQUEST_PATH = 0x00000002;

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_SetSearchW(string lpSearchString);

        [DllImport("Everything64.dll")]
        private static extern void Everything_SetMax(uint dwMax);

        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(uint dwRequestFlags);

        [DllImport("Everything64.dll")]
        private static extern bool Everything_QueryW(bool bWait);

        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetNumResults();

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathNameW(uint index, StringBuilder lpString, uint maxCount);

        [DllImport("Everything64.dll")]
        private static extern bool Everything_IsDBLoaded();

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Task.FromResult<IEnumerable<SearchResult>>(results);

            // Don't run full-disk search if user is querying AI
            if (query.StartsWith("ai ", StringComparison.OrdinalIgnoreCase) || 
                query.StartsWith("ai:", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<IEnumerable<SearchResult>>(results);
            }

            try
            {
                if (!Everything_IsDBLoaded()) return Task.FromResult<IEnumerable<SearchResult>>(results);

                Everything_SetSearchW(query);
                Everything_SetMax(10); // Return top 10 fastest file matches
                Everything_SetRequestFlags(EVERYTHING_REQUEST_FILE_NAME | EVERYTHING_REQUEST_PATH);
                Everything_QueryW(true);

                uint numResults = Everything_GetNumResults();
                StringBuilder sb = new StringBuilder(1024);

                for (uint i = 0; i < numResults; i++)
                {
                    Everything_GetResultFullPathNameW(i, sb, 1024);
                    string fullPath = sb.ToString();
                    string fileName = Path.GetFileName(fullPath);

                    results.Add(new SearchResult
                    {
                        Title = $"📄 {fileName}",
                        Subtitle = fullPath,
                        ExecutionPath = fullPath,
                        Score = 75 - (int)i
                    });
                }
            }
            catch
            {
                // Gracefully pass if Everything service/DLL isn't running
            }

            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }
    }
}