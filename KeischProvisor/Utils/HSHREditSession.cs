using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Respectre.Utils
{
    public sealed class HSHRReplacementSubheader
    {
        public string Name { get; }
        public uint NameHash { get; }
        public byte[] Header { get; }

        public HSHRReplacementSubheader(string name, uint nameHash, byte[] header)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NameHash = nameHash;
            Header = header == null ? throw new ArgumentNullException(nameof(header)) : (byte[])header.Clone();
        }
    }

    public sealed class HSHRReplacementData
    {
        public byte[] Header { get; }
        public IReadOnlyList<HSHRReplacementSubheader> Subheaders { get; }

        public HSHRReplacementData(byte[] header, IEnumerable<HSHRReplacementSubheader> subheaders)
        {
            Header = header == null ? throw new ArgumentNullException(nameof(header)) : (byte[])header.Clone();
            Subheaders = (subheaders ?? throw new ArgumentNullException(nameof(subheaders))).ToArray();
        }
    }

    public sealed class HSHREditSession
    {
        public HSHRFile Original { get; private set; }
        public HSHRFile Current { get; private set; }
        public byte[] OriginalBinary { get; private set; }
        public byte[] CurrentBinary { get; private set; }

        public bool IsDirty => GetChangedSlots().Count > 0;

        public HSHREditSession(HSHRFile source, byte[] sourceBinary)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sourceBinary == null) throw new ArgumentNullException(nameof(sourceBinary));
            Original = Clone(source);
            Current = Clone(source);
            OriginalBinary = (byte[])sourceBinary.Clone();
            CurrentBinary = (byte[])sourceBinary.Clone();
        }

        public List<int> GetChangedSlots()
        {
            int count = Math.Min(Original.Data.Count, Current.Data.Count);
            var changed = new List<int>();
            for (int i = 0; i < count; i++)
                if (Original.MainIndex[i].NameHash != Current.MainIndex[i].NameHash
                    || !DataEquals(Original.Data[i], Current.Data[i])) changed.Add(i);
            for (int i = count; i < Math.Max(Original.Data.Count, Current.Data.Count); i++) changed.Add(i);
            return changed;
        }

        public bool IsSlotModified(int slot) =>
            slot >= 0 && slot < Original.Data.Count && slot < Current.Data.Count
            && (Original.MainIndex[slot].NameHash != Current.MainIndex[slot].NameHash
                || !DataEquals(Original.Data[slot], Current.Data[slot]));

        public bool IsSubheaderModified(int slot, int subIndex)
        {
            if (!IsSlotModified(slot)) return false;
            var before = Original.Data[slot];
            var after = Current.Data[slot];
            if (subIndex < 0 || subIndex >= before.Subheader.Count || subIndex >= after.Subheader.Count
                || subIndex >= before.SubheaderIndex.Count || subIndex >= after.SubheaderIndex.Count)
                return true;
            return before.SubheaderIndex[subIndex].NameHash != after.SubheaderIndex[subIndex].NameHash
                || !BytesEqual(before.Subheader[subIndex], after.Subheader[subIndex]);
        }

        public void ReplaceSlot(int slotIndex, string targetName, uint targetNameHash, HSHRReplacementData replacement)
        {
            if (slotIndex < 0 || slotIndex >= Current.Data.Count || slotIndex >= Current.MainIndex.Count)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            if (string.IsNullOrWhiteSpace(targetName))
                throw new ArgumentException("A top-header name is required.", nameof(targetName));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            if (replacement.Header.Length < 16)
                throw new InvalidDataException("The selected RPF has an incomplete root header.");

            var indexes = new List<HSHRIndex>();
            var subheaders = new List<byte[]>();
            var paths = new List<string>();
            foreach (var child in replacement.Subheaders)
            {
                if (child.Header.Length < 16)
                    throw new InvalidDataException($"The child RPF '{child.Name}' has no valid raw header.");
                indexes.Add(new HSHRIndex { NameHash = child.NameHash, DataOffset = 0 });
                subheaders.Add((byte[])child.Header.Clone());
                paths.Add(child.Name);
            }

            var candidate = Clone(Current);
            candidate.MainIndex[slotIndex].NameHash = targetNameHash;
            candidate.TopHeaderNames[slotIndex] = targetName;
            var target = candidate.Data[slotIndex];
            target.Header = (byte[])replacement.Header.Clone();
            target.SubheaderIndex = indexes;
            target.Subheader = subheaders;
            target.SubheaderPaths = paths;
            target.SubheadersCount = (uint)indexes.Count;

            byte[] rebuilt = candidate.ReKReateHSHR();
            var verified = ParseBinary(rebuilt, candidate.FilePath);
            verified.TopHeaderNames = new List<string>(candidate.TopHeaderNames);
            verified.Data[slotIndex].SubheaderPaths = paths;
            Current = verified;
            CurrentBinary = rebuilt;
        }

        public byte[] BuildBinary()
        {
            byte[] binary = Current.ReKReateHSHR();
            _ = ParseBinary(binary, Current.FilePath);
            return binary;
        }

        public void AcceptSaved(string path, byte[] binary)
        {
            if (binary == null || binary.Length < 16)
                throw new ArgumentException("A complete HSHR binary is required.", nameof(binary));
            Current.FilePath = path;
            Current.Name = Path.GetFileName(path);
            Current.FileSize = binary.LongLength;
            Current.Checksum = BitConverter.ToUInt64(binary, 8);
            Original = Clone(Current);
            OriginalBinary = (byte[])binary.Clone();
            CurrentBinary = (byte[])binary.Clone();
        }

        private static HSHRFile ParseBinary(byte[] binary, string path)
        {
            var parsed = new HSHRFile
            {
                FilePath = path,
                Name = Path.GetFileName(path),
            };
            parsed.ScanStructure(binary);
            if (parsed.Signature != 0x52485348 || parsed.MainIndex.Count != parsed.TopHeadersCount
                || parsed.Data.Count != parsed.TopHeadersCount)
                throw new InvalidDataException("The rebuilt data is not a complete HSHR cache.");
            return parsed;
        }

        private static HSHRFile Clone(HSHRFile source)
        {
            return new HSHRFile
            {
                Name = source.Name,
                FilePath = source.FilePath,
                FileSize = source.FileSize,
                Signature = source.Signature,
                Version = source.Version,
                Checksum = source.Checksum,
                TopHeadersCount = source.TopHeadersCount,
                VersionMajor = source.VersionMajor,
                VersionMinor = source.VersionMinor,
                MainIndex = source.MainIndex.Select(i => new HSHRIndex
                {
                    NameHash = i.NameHash,
                    DataOffset = i.DataOffset,
                }).ToList(),
                TopHeaderNames = new List<string>(source.TopHeaderNames),
                Data = source.Data.Select(d => new HSHRData
                {
                    CSTTMarker = d.CSTTMarker,
                    HeaderSize = d.HeaderSize,
                    AllDataSize = d.AllDataSize,
                    Header = (byte[])d.Header.Clone(),
                    SubheadersCount = d.SubheadersCount,
                    IDSTMarker = d.IDSTMarker,
                    IDENMarker = d.IDENMarker,
                    SubheaderIndex = d.SubheaderIndex.Select(i => new HSHRIndex
                    {
                        NameHash = i.NameHash,
                        DataOffset = i.DataOffset,
                    }).ToList(),
                    Subheader = d.Subheader.Select(b => (byte[])b.Clone()).ToList(),
                    SubheaderPaths = new List<string>(d.SubheaderPaths),
                    BlockStart = d.BlockStart,
                    HeaderMagicOffset = d.HeaderMagicOffset,
                }).ToList(),
            };
        }

        private static bool DataEquals(HSHRData left, HSHRData right)
        {
            if (!BytesEqual(left.Header, right.Header)
                || left.SubheaderIndex.Count != right.SubheaderIndex.Count
                || left.Subheader.Count != right.Subheader.Count)
                return false;

            for (int i = 0; i < left.SubheaderIndex.Count; i++)
                if (left.SubheaderIndex[i].NameHash != right.SubheaderIndex[i].NameHash) return false;
            for (int i = 0; i < left.Subheader.Count; i++)
                if (!BytesEqual(left.Subheader[i], right.Subheader[i])) return false;
            return true;
        }

        private static bool BytesEqual(byte[] left, byte[] right) =>
            left.AsSpan().SequenceEqual(right);
    }
}
