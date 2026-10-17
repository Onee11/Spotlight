using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SpotlightWin.Infrastructure.Services
{
    public static class FileSearchService
    {
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFileExW(
            string lpFileName,
            int fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            int fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private static readonly HashSet<string> SkipFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "$Recycle.Bin", "System Volume Information", "WinSxS", "node_modules", ".git"
        };

        public static async Task<List<string>> SearchAllDrivesAsync(
            string searchTerm,
            List<string>? allowedDrives = null,
            List<string>? excludedDrives = null,
            int maxResults = 30,
            CancellationToken token = default)
        {
            var results = new ConcurrentBag<string>();
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2) return results.ToList();

            var drivesToScan = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .Select(d => d.Name.TrimEnd('\\').ToUpper())
                .ToList();

            if (allowedDrives != null && allowedDrives.Count > 0)
            {
                var allowedUpper = allowedDrives.Select(d => d.TrimEnd('\\').ToUpper()).ToList();
                drivesToScan = drivesToScan.Where(d => allowedUpper.Contains(d)).ToList();
            }

            if (excludedDrives != null && excludedDrives.Count > 0)
            {
                var excludedUpper = excludedDrives.Select(d => d.TrimEnd('\\').ToUpper()).ToList();
                drivesToScan = drivesToScan.Where(d => !excludedUpper.Contains(d)).ToList();
            }

            await Task.Run(() =>
            {
                Parallel.ForEach(drivesToScan, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, drive =>
                {
                    if (results.Count >= maxResults || token.IsCancellationRequested) return;
                    ScanNativeWin32(drive + "\\", searchTerm, results, maxResults, token);
                });
            }, token);

            return results.Take(maxResults).ToList();
        }

        private static void ScanNativeWin32(string rootDirectory, string searchTerm, ConcurrentBag<string> results, int maxResults, CancellationToken token)
        {
            var queue = new Queue<string>();
            queue.Enqueue(rootDirectory);

            while (queue.Count > 0 && results.Count < maxResults && !token.IsCancellationRequested)
            {
                string currentDir = queue.Dequeue();
                string searchPattern = System.IO.Path.Combine(currentDir, "*");

                IntPtr hFind = FindFirstFileExW(searchPattern, 1, out WIN32_FIND_DATA findData, 0, IntPtr.Zero, 2);
                if (hFind == INVALID_HANDLE_VALUE) continue;

                try
                {
                    do
                    {
                        if (results.Count >= maxResults || token.IsCancellationRequested) break;

                        string fileName = findData.cFileName;
                        if (fileName == "." || fileName == "..") continue;

                        bool isDir = (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
                        bool isReparsePoint = (findData.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0;

                        string fullPath = System.IO.Path.Combine(currentDir, fileName);

                        if (fileName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            results.Add(fullPath);
                        }

                        if (isDir && !isReparsePoint && !SkipFolders.Contains(fileName))
                        {
                            queue.Enqueue(fullPath);
                        }
                    }
                    while (FindNextFileW(hFind, out findData));
                }
                finally
                {
                    FindClose(hFind);
                }
            }
        }
    }
}