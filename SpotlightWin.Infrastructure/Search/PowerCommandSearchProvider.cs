using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SpotlightWin.Core.Interfaces;
using SpotlightWin.Core.Models;

namespace SpotlightWin.Infrastructure.Search
{
    public class PowerCommandSearchProvider : ISearchProvider
    {
        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;

        private readonly List<PowerCommand> _commands = new();

        public Task InitializeAsync()
        {
            _commands.Add(new PowerCommand("Lock PC", "Lock your Windows session", "cmd:lock", "🔒"));
            _commands.Add(new PowerCommand("Empty Recycle Bin", "Permanently delete files in Recycle Bin", "cmd:recycle", "🗑️"));
            _commands.Add(new PowerCommand("Shutdown PC", "Shut down Windows immediately", "cmd:shutdown", "⚡"));
            _commands.Add(new PowerCommand("Restart PC", "Restart Windows", "cmd:restart", "🔄"));
            _commands.Add(new PowerCommand("Sleep PC", "Put computer into sleep mode", "cmd:sleep", "🌙"));

            return Task.CompletedTask;
        }

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Task.FromResult<IEnumerable<SearchResult>>(results);
            }

            string cleanQuery = query.Trim().ToLowerInvariant();

            foreach (var cmd in _commands)
            {
                if (cmd.Name.ToLowerInvariant().Contains(cleanQuery) || 
                    cmd.ActionKey.ToLowerInvariant().Contains(cleanQuery))
                {
                    results.Add(new SearchResult
                    {
                        Title = $"{cmd.Icon} {cmd.Name}",
                        Subtitle = $"System Command • {cmd.Description}",
                        ExecutionPath = cmd.ActionKey,
                        Score = 95 // Give high priority when user types system commands
                    });
                }
            }

            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }

        private class PowerCommand
        {
            public string Name { get; }
            public string Description { get; }
            public string ActionKey { get; }
            public string Icon { get; }

            public PowerCommand(string name, string description, string actionKey, string icon)
            {
                Name = name;
                Description = description;
                ActionKey = actionKey;
                Icon = icon;
            }
        }
    }
}