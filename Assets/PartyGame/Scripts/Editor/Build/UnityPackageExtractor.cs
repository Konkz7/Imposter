using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace PartyGame.EditorTools
{
    /// <summary>
    /// Extracts a .unitypackage straight to disk, preserving each asset's original .meta and
    /// therefore its GUID.
    ///
    /// Unity's own importer runs asynchronously, which means it never finishes inside a
    /// batch-mode run that quits immediately. Doing it synchronously here is what lets the
    /// project be set up from the command line in one pass.
    /// </summary>
    public static class UnityPackageExtractor
    {
        private class Entry
        {
            public string PathName;
            public byte[] Asset;
            public byte[] Meta;
        }

        /// <summary>Returns the number of assets written.</summary>
        public static int Extract(string packagePath, string projectRoot)
        {
            if (!File.Exists(packagePath))
            {
                Debug.LogError("[Party Game] Package not found: " + packagePath);
                return 0;
            }

            var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

            using (var file = File.OpenRead(packagePath))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                foreach (var record in ReadTar(gzip))
                {
                    var separator = record.Name.IndexOf('/');
                    if (separator <= 0) continue;

                    var guid = record.Name.Substring(0, separator);
                    var kind = record.Name.Substring(separator + 1).TrimEnd('/');
                    if (kind.Length == 0) continue;

                    if (!entries.TryGetValue(guid, out var entry))
                    {
                        entry = new Entry();
                        entries[guid] = entry;
                    }

                    switch (kind)
                    {
                        case "pathname":
                            entry.PathName = Encoding.UTF8.GetString(record.Data)
                                .Split('\n')[0].Trim().Replace('\\', '/');
                            break;
                        case "asset":
                            entry.Asset = record.Data;
                            break;
                        case "asset.meta":
                            entry.Meta = record.Data;
                            break;
                    }
                }
            }

            var written = 0;
            foreach (var entry in entries.Values)
            {
                if (string.IsNullOrEmpty(entry.PathName)) continue;
                if (!entry.PathName.StartsWith("Assets/", StringComparison.Ordinal)) continue;

                var target = Path.Combine(projectRoot, entry.PathName);
                var directory = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                if (entry.Asset != null)
                {
                    File.WriteAllBytes(target, entry.Asset);
                    written++;
                }
                else
                {
                    // A folder entry: the path itself is the directory.
                    Directory.CreateDirectory(target);
                }

                if (entry.Meta != null) File.WriteAllBytes(target + ".meta", entry.Meta);
            }

            return written;
        }

        private struct TarRecord
        {
            public string Name;
            public byte[] Data;
        }

        private static IEnumerable<TarRecord> ReadTar(Stream stream)
        {
            var header = new byte[512];

            while (true)
            {
                if (!ReadExactly(stream, header, 512)) yield break;

                // Two consecutive zero blocks mark the end of the archive.
                var empty = true;
                for (var i = 0; i < 512; i++)
                {
                    if (header[i] == 0) continue;
                    empty = false;
                    break;
                }
                if (empty) yield break;

                var name = Encoding.UTF8.GetString(header, 0, 100).TrimEnd('\0', ' ');
                var sizeText = Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' ');
                var size = 0L;
                foreach (var c in sizeText)
                {
                    if (c < '0' || c > '7') break;
                    size = size * 8 + (c - '0');
                }

                var typeFlag = (char)header[156];
                var data = new byte[size];
                if (size > 0 && !ReadExactly(stream, data, (int)size)) yield break;

                // Records are padded to a 512 byte boundary.
                var padding = (int)((512 - (size % 512)) % 512);
                if (padding > 0)
                {
                    var skip = new byte[padding];
                    if (!ReadExactly(stream, skip, padding)) yield break;
                }

                if (typeFlag == '0' || typeFlag == '\0' || typeFlag == '5')
                    yield return new TarRecord { Name = name, Data = data };
            }
        }

        private static bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) return false;
                offset += read;
            }
            return true;
        }
    }
}
