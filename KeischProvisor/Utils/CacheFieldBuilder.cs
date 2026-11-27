using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Respectre.Utils
{
    /// <summary>
    /// Builds <see cref="FieldDescriptor"/> lists for each kind of rpf.cache node.
    ///
    /// Every field that the previous UI displayed is represented here, so the redesigned
    /// explorer can render the same information from a single source of truth:
    ///   - summary section  (FieldKind.Computed)
    ///   - raw field table  (FieldKind.Raw / Marker / Pointer / IndexEntry)
    ///   - hex viewer highlight (Offset + Length)
    /// </summary>
    public sealed class CacheFieldBuilder
    {
        private readonly HSHRFile _file;
        private readonly byte[] _binary;
        private readonly uint _topCount;

        public CacheFieldBuilder(HSHRFile file, byte[] binary)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _binary = binary ?? throw new ArgumentNullException(nameof(binary));
            _topCount = file.TopHeadersCount;
        }

        // ---- helpers --------------------------------------------------------

        private byte[]? Slice(long offset, int length)
        {
            if (offset < 0 || length <= 0) return null;
            if (offset + length > _binary.LongLength) return null;
            return _binary.AsSpan((int)offset, length).ToArray();
        }

        private FieldDescriptor Raw(string name, string type, long offset, int length,
                                    string? desc = null, string? unit = null)
        {
            var fd = new FieldDescriptor
            {
                Name = name,
                Type = type,
                Offset = offset,
                Length = length,
                Description = desc,
                Unit = unit,
                Kind = FieldKind.Raw,
            };
            fd.Bytes = Slice(offset, length);
            return fd;
        }

        private FieldDescriptor Computed(string name, string type, string value, string? desc = null)
        {
            // Computed fields have no byte range in the file; they carry a pre-rendered value.
            return new FieldDescriptor
            {
                Name = name,
                Type = type,
                Offset = -1,
                Length = 0,
                Description = desc,
                Kind = FieldKind.Computed,
                Status = FieldStatus.Ok,
                StatusDetail = value,
            };
        }

        private FieldDescriptor Marker(string name, long offset, uint expected, string? desc = null)
        {
            var fd = Raw(name, "u32", offset, 4, desc ?? $"Magic marker, must equal 0x{expected:X8}");
            var u = fd.Unsigned;
            if (u.HasValue)
            {
                fd.Status = u.Value == expected ? FieldStatus.Ok : FieldStatus.Error;
                fd.StatusDetail = u.Value == expected
                    ? $"== 0x{expected:X8}"
                    : $"expected 0x{expected:X8}, found 0x{u.Value:X8}";
            }
            else
            {
                fd.Status = FieldStatus.Error;
                fd.StatusDetail = "unreadable";
            }
            fd.Kind = FieldKind.Marker;
            return fd;
        }

        private static string EncryptionName(uint enc) => enc switch
        {
            0x4E45504F => "OPEN",
            0x0FFFFFF9 => "AES",
            0x0FEFFFFF => "NG",
            0 => "NONE",
            _ => "unknown",
        };

        // ---- root -----------------------------------------------------------

        public List<FieldDescriptor> BuildRoot()
        {
            var list = new List<FieldDescriptor>();

            var sig = Marker("signature", 0x00, 0x52485348, "File signature, 'HSHR'");
            list.Add(sig);

            var ver = Raw("version", "u32", 0x04, 4, "Build stamp: major in high 16 bits, minor in low 16 bits");
            var verU = ver.Unsigned;
            if (verU.HasValue)
            {
                ushort major = (ushort)(verU.Value >> 16);
                ushort minor = (ushort)(verU.Value & 0xFFFF);
                ver.Status = FieldStatus.Ok;
                ver.StatusDetail = $"{major}.{minor}";
                _file.VersionMajor = (short)major;
                _file.VersionMinor = (short)minor;
            }
            list.Add(ver);

            var chk = Raw("checksum", "u64", 0x08, 8, "CityHash64 variant over the whole file with the implied seed");
            list.Add(chk);

            var top = Raw("top_headers_count", "u32", 0x10, 4, "Number of CSTT blocks (and main index entries)", "entries");
            if (top.Unsigned.HasValue)
            {
                top.Status = top.Unsigned.Value == _topCount ? FieldStatus.Ok : FieldStatus.Warning;
                top.StatusDetail = top.Unsigned.Value == _topCount ? "== scanned count" : "differs from scanned count";
            }
            list.Add(top);

            // Summary (interpreted) — mirrors what the old toolbar showed.
            list.Add(Computed("Signature", "ascii[4]", "HSHR"));
            if (verU.HasValue)
                list.Add(Computed("Build", "major.minor",
                    $"{(ushort)(verU.Value >> 16)}.{(ushort)(verU.Value & 0xFFFF)}"));
            // Hex only here; the full decimal value is available in the raw field table so the
            // summary stays one short line. Tooltip carries both forms.
            list.Add(Computed("Validation Token", "u64",
                $"0x{chk.Hex}  ({chk.Dec})"));
            list.Add(Computed("Top Headers Count", "u32", _topCount.ToString()));

            return list;
        }

        // ---- main index -----------------------------------------------------

        /// <summary>Main index entries; each pair is {name_hash, data_offset}.</summary>
        public List<FieldDescriptor> BuildMainIndex()
        {
            var list = new List<FieldDescriptor>();
            for (int i = 0; i < _topCount && i < _file.MainIndex.Count; i++)
            {
                long off = 0x14 + i * 8L;
                var hash = Raw($"main_index[{i}].name_hash", "u32", off, 4, "JOAAT of the lowercase archive path");
                hash.Kind = FieldKind.IndexEntry;
                var resolved = CacheTreeBuilder.ResolveTopName(_file, i);
                hash.Status = FieldStatus.Ok;
                hash.StatusDetail = resolved;
                list.Add(hash);

                var dataOff = Raw($"main_index[{i}].data_offset", "u32", off + 4, 4,
                    "Absolute offset of the '7FPR' magic inside this block");
                dataOff.Kind = FieldKind.Pointer;
                var dv = dataOff.Unsigned;
                if (dv.HasValue)
                {
                    bool points = dv.Value + 4UL <= (ulong)_binary.LongLength
                        && _binary[dv.Value] == (byte)'7' && _binary[dv.Value + 1] == (byte)'F'
                        && _binary[dv.Value + 2] == (byte)'P' && _binary[dv.Value + 3] == (byte)'R';
                    dataOff.Status = points ? FieldStatus.Ok : FieldStatus.Error;
                    dataOff.StatusDetail = points ? "-> 7FPR" : "does not point at 7FPR";
                }
                list.Add(dataOff);
            }
            return list;
        }

        // ---- top-level header block ----------------------------------------

        public List<FieldDescriptor> BuildTopHeader(int index)
        {
            var list = new List<FieldDescriptor>();
            if (index < 0 || index >= _file.Data.Count) return list;
            var d = _file.Data[index];
            long bs = d.BlockStart;

            list.Add(Marker("CSTT", bs, 0x43535454, "Block start marker"));

            var a = Raw("header_size", "u32", bs + 4, 4,
                "Size of the cached RPF header payload, including front and post padding", "bytes");
            a.Status = a.Unsigned.HasValue && a.Unsigned.Value == d.HeaderSize
                ? FieldStatus.Ok : FieldStatus.Warning;
            a.StatusDetail = $"IDST sits at blockStart + A + 0x10 = 0x{(bs + 12 + d.HeaderSize + 4):X}";
            list.Add(a);

            var b = Raw("all_data_size", "u32", bs + 8, 4,
                "Payload size; next CSTT = blockStart + 12 + B", "bytes");
            b.Status = b.Unsigned.HasValue && b.Unsigned.Value == d.AllDataSize
                ? FieldStatus.Ok : FieldStatus.Warning;
            b.StatusDetail = $"next block at 0x{bs + 12 + d.AllDataSize:X}";
            list.Add(b);

            if (d.HeaderMagicOffset >= 0)
            {
                long frontPad = d.HeaderMagicOffset - (bs + 12);
                if (frontPad > 0)
                {
                    var pad = Raw("front_padding", "byte[A]", bs + 12, (int)frontPad,
                        "Zero padding that aligns the 7FPR magic to a 16-byte absolute offset");
                    pad.Status = FieldStatus.Ok;
                    pad.StatusDetail = $"{frontPad} bytes";
                    list.Add(pad);
                }

                long m = d.HeaderMagicOffset;
                list.Add(Marker("7FPR", m, 0x52504637, "Cached root RPF header magic"));

                if (d.Header != null && d.Header.Length >= 16)
                {
                    var ec = Raw("entryCount", "u32", m + 4, 4, "Number of TOC entries in the cached RPF header", "entries");
                    ec.Kind = FieldKind.Raw;
                    list.Add(ec);

                    var nl = Raw("namesLength", "u32", m + 8, 4, "Name table length (includes internal 16-byte padding)", "bytes");
                    list.Add(nl);

                    var enc = Raw("encryption", "u32", m + 12, 4, "TOC encryption mode");
                    if (enc.Unsigned.HasValue)
                    {
                        enc.Status = FieldStatus.Ok;
                        enc.StatusDetail = $"{EncryptionName((uint)enc.Unsigned.Value)}";
                    }
                    list.Add(enc);

                    // Post padding after the real header: entryCount * 2 bytes (observed rule).
                    long realLen = 16L + (long)d.EntryCount * 16 + d.NamesLength;
                    long postPad = d.HeaderSize - frontPad - realLen;
                    if (postPad > 0)
                    {
                        var pp = Raw("post_padding", "byte[entryCount*2]", m + realLen, (int)postPad,
                            "Trailing zero padding; engine crashes on load when this is too short");
                        pp.Status = postPad == (long)d.EntryCount * 2 ? FieldStatus.Ok : FieldStatus.Warning;
                        pp.StatusDetail = $"{(long)d.EntryCount * 2} bytes expected, {postPad} present";
                        list.Add(pp);
                    }
                }
            }

            long nPos = bs + 12 + d.HeaderSize;
            var cnt = Raw("subheaders_count", "u32", nPos, 4, "Number of subheader index entries", "entries");
            cnt.Status = cnt.Unsigned.HasValue && cnt.Unsigned.Value == d.SubheadersCount
                ? FieldStatus.Ok : FieldStatus.Warning;
            list.Add(cnt);

            list.Add(Marker("IDST", nPos + 4, 0x49445354, "Subheader index start marker"));

            long idxStart = nPos + 8;
            for (int j = 0; j < d.SubheadersCount && j < d.SubheaderIndex.Count; j++)
            {
                long eo = idxStart + j * 8L;
                var hash = Raw($"subheaders_index[{j}].name_hash", "u32", eo, 4,
                    "JOAAT of the subheader's relative path");
                hash.Kind = FieldKind.IndexEntry;
                hash.Status = FieldStatus.Ok;
                hash.StatusDetail = string.IsNullOrEmpty(d.SubheaderPath(j)) ? null : d.SubheaderPath(j);
                list.Add(hash);

                var off = Raw($"subheaders_index[{j}].data_offset", "u32", eo + 4, 4,
                    "Absolute offset of this subheader's '7FPR' magic");
                off.Kind = FieldKind.Pointer;
                var ov = off.Unsigned;
                if (ov.HasValue)
                {
                    bool points = ov.Value + 4UL <= (ulong)_binary.LongLength
                        && _binary[ov.Value] == (byte)'7' && _binary[ov.Value + 1] == (byte)'F'
                        && _binary[ov.Value + 2] == (byte)'P' && _binary[ov.Value + 3] == (byte)'R';
                    off.Status = points ? FieldStatus.Ok : FieldStatus.Error;
                    off.StatusDetail = points ? "-> 7FPR" : "does not point at 7FPR";
                }
                list.Add(off);
            }

            list.Add(Marker("IDEN", d.IDENOffset, 0x4944454E, "Subheader index end marker"));

            // ---- summary (interpreted) ----
            int cwSubs = d.Subheader?.Count ?? 0;
            bool consistent = d.SubheadersCount == d.SubheaderIndex.Count && d.SubheadersCount == cwSubs;
            list.Add(Computed("Resolved Name",
                "string",
                CacheTreeBuilder.ResolveTopName(_file, index)));
            list.Add(Computed("Name Hash (Hex)", "u32", $"0x{_file.MainIndex[index].NameHash:X8}"));
            list.Add(Computed("Data Offset (Hex)", "u32", $"0x{_file.MainIndex[index].DataOffset:X}"));
            list.Add(Computed("Header Size", "u32", $"{d.HeaderSize} bytes"));
            list.Add(Computed("All Data Size", "u32", $"{d.AllDataSize} bytes"));
            list.Add(Computed("Subheaders Count", "u32", d.SubheadersCount.ToString()));
            list.Add(Computed("Subheader Consistency", "bool", consistent ? "Valid" : "Invalid"));
            list.Add(Computed("Block Range", "range", $"0x{bs:X} - 0x{d.BlockEnd:X} ({d.BlockEnd - bs} bytes)"));

            return list;
        }

        // ---- subheader -------------------------------------------------------

        public List<FieldDescriptor> BuildSubheader(int topIndex, int subIndex)
        {
            var list = new List<FieldDescriptor>();
            if (topIndex < 0 || topIndex >= _file.Data.Count) return list;
            var d = _file.Data[topIndex];
            if (subIndex < 0 || subIndex >= d.SubheaderIndex.Count) return list;

            long m = d.SubheaderIndex[subIndex].DataOffset;
            list.Add(Marker("7FPR", m, 0x52504637, "Cached nested RPF header magic"));

            byte[]? sub = (subIndex < d.Subheader.Count) ? d.Subheader[subIndex] : null;
            if (sub != null && sub.Length >= 16)
            {
                list.Add(Raw("entryCount", "u32", m + 4, 4, "TOC entries in this subheader", "entries"));
                list.Add(Raw("namesLength", "u32", m + 8, 4, "Name table length", "bytes"));
                var enc = Raw("encryption", "u32", m + 12, 4, "TOC encryption mode");
                if (enc.Unsigned.HasValue)
                {
                    enc.Status = FieldStatus.Ok;
                    enc.StatusDetail = EncryptionName((uint)enc.Unsigned.Value);
                }
                list.Add(enc);
            }

            // ---- summary ----
            var idxEntry = d.SubheaderIndex[subIndex];
            list.Add(Computed("Name Hash (Hex)", "u32", $"0x{idxEntry.NameHash:X8}"));
            list.Add(Computed("Data Offset (Hex)", "u32", $"0x{idxEntry.DataOffset:X}"));
            list.Add(Computed("Path", "string",
                string.IsNullOrEmpty(d.SubheaderPath(subIndex)) ? "Unknown" : d.SubheaderPath(subIndex)!));
            list.Add(Computed("Header Size", "u32", sub != null ? $"{sub.Length} bytes" : "—"));
            list.Add(Computed("Parent", "string", $"#{topIndex}"));

            return list;
        }
    }
}

