using System.Collections.Generic;
using System.Threading.Tasks;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Core.Interfaces
{
    /// <summary>
    /// Interface for search result providers.
    /// </summary>
    public interface ISearchProvider
    {
        Task InitializeAsync();
        Task<IEnumerable<SearchResult>> SearchAsync(string query);
    }
}