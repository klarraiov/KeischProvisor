using System;
using System.Collections.Generic;
using System.Linq;

namespace Respectre.Utils
{
    /// <summary>
    /// Flattens an <see cref="HSHRFile"/> into the node tree shown in the left explorer pane.
    ///
    /// Hierarchy mirrors the on-disk containment:
    ///   Root
    ///     └─ TopHeader (#0 … #N)      one CSTT block each
    ///          └─ Subheader ([0] … [N]) one cached nested-RPF header each
    /// </summary>
    public static class CacheTreeBuilder
    {
        public static CacheNode Build(HSHRFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var root = new CacheNode
            {
                Kind = CacheNodeKind.Root,
                Label = "",          // the root row shows only the file name; no index column
                Name = file.Name ?? "rpf.cache",
                Start = 0,
                End = file.FileSize,
            };

            int topCount = (int)Math.Min(file.TopHeadersCount, (uint)file.Data.Count);
            for (int i = 0; i < topCount; i++)
            {
                var data = file.Data[i];
                var top = new CacheNode
                {
                    Kind = CacheNodeKind.TopHeader,
                    Label = $"#{i}",
                    Name = ResolveTopName(file, i),
                    Start = data.BlockStart,
                    End = data.BlockStart + 12 + data.AllDataSize,
                    TopIndex = i,
                };

                int subCount = Math.Min(data.SubheaderIndex.Count, data.Subheader.Count);
                for (int j = 0; j < subCount; j++)
                {
                    var idx = data.SubheaderIndex[j];
                    long start = idx.DataOffset;
                    long len = (j < data.Subheader.Count && data.Subheader[j] != null)
                        ? data.Subheader[j].Length : 0;
                    top.Children.Add(new CacheNode
                    {
                        Kind = CacheNodeKind.Subheader,
                        Label = $"[{j}]",
                        Name = data.SubheaderPath(j) ?? $"0x{idx.NameHash:X8}",
                        Start = start,
                        End = start + len,
                        TopIndex = i,
                        SubIndex = j,
                    });
                }

                root.Children.Add(top);
            }

            return root;
        }

        /// <summary>
        /// Resolves a top-level header name. Falls back to the raw hash so that no
        /// information is lost when the archive is not present on disk.
        /// </summary>
        public static string ResolveTopName(HSHRFile file, int index)
        {
            if (index < 0 || index >= file.MainIndex.Count) return "—";
            if (index < file.TopHeaderNames.Count && !string.IsNullOrEmpty(file.TopHeaderNames[index]))
                return file.TopHeaderNames[index];
            uint hash = file.MainIndex[index].NameHash;
            return HSHRFile.namehashPairs.TryGetValue(hash, out var name)
                ? name
                : $"Unknown (0x{hash:X8})";
        }

        /// <summary>Flattened pre-order enumeration, used for virtualized list rendering.</summary>
        public static IEnumerable<(CacheNode Node, int Depth)> Flatten(CacheNode root)
        {
            var stack = new Stack<(CacheNode, int)>();
            stack.Push((root, 0));
            while (stack.Count > 0)
            {
                var (node, depth) = stack.Pop();
                yield return (node, depth);
                for (int i = node.Children.Count - 1; i >= 0; i--)
                    stack.Push((node.Children[i], depth + 1));
            }
        }
    }
}
