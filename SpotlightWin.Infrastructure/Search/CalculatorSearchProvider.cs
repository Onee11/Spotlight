using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Infrastructure.Search
{
    public class CalculatorSearchProvider : ISearchProvider
    {
        // Simple regex to check if query contains mathematical characters
        private static readonly Regex MathRegex = new(@"^[0-9\.\+\-\*\/\(\)\^\s\%]+$", RegexOptions.Compiled);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(query) || !MathRegex.IsMatch(query) || !ContainsOperators(query))
            {
                return Task.FromResult<IEnumerable<SearchResult>>(results);
            }

            try
            {
                // Safely evaluate math expression using DataTable.Compute
                using var dt = new DataTable();
                var computeResult = dt.Compute(query, null);

                if (computeResult != null && computeResult != DBNull.Value)
                {
                    string resultString = computeResult.ToString() ?? string.Empty;

                    results.Add(new SearchResult
                    {
                        Title = $"= {resultString}",
                        Subtitle = $"Calculator: Press Enter to copy '{resultString}' to clipboard",
                        ExecutionPath = $"copy:{resultString}",
                        Score = 1000 // Highest priority
                    });
                }
            }
            catch
            {
                // Incomplete math expression (e.g., user is still typing "12 + ")
            }

            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }

        private static bool ContainsOperators(string query)
        {
            return query.Contains('+') || query.Contains('-') || 
                   query.Contains('*') || query.Contains('/') || 
                   query.Contains('%');
        }
    }
}