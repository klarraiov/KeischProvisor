using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Respectre.Utils
{
    /// <summary>
    /// How a field should be rendered in the summary (interpreted) area vs the raw field table.
    /// </summary>
    public enum FieldKind
    {
        /// <summary>Container/step field: CSTT marker, IDST marker, padding.</summary>
        Marker,
        /// <summary>Interpreted/derived value shown in the summary section (B).</summary>
        Computed,
        /// <summary>Raw value stored in the file, shown in the field table (C).</summary>
        Raw,
        /// <summary>Absolute file offset pointer to another structure.</summary>
        Pointer,
        /// <summary>Repeating index entry (name_hash / data_offset).</summary>
        IndexEntry,
    }

    public enum FieldStatus
    {
        Unknown,
        Ok,
        Warning,
        Error,
        /// <summary>No validation rule applies (informational only).</summary>
        NotApplicable,
    }

    /// <summary>
    /// A single addressable field inside rpf.cache. One descriptor list per node drives the
    /// summary, the raw field table and the hex viewer highlight at once, so the three areas
    /// can never disagree about offsets or values.
    /// </summary>
    public sealed class FieldDescriptor
    {
        /// <summary>Display name, e.g. "header_size" or "CSTT".</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Data type label, e.g. "u32", "u64", "byte[A]", "ascii[4]".</summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>Human readable explanation.</summary>
        public string? Description { get; init; }

        /// <summary>Absolute file offset of the first byte of this field.</summary>
        public long Offset { get; init; }

        /// <summary>Length in bytes.</summary>
        public int Length { get; init; }

        public FieldKind Kind { get; set; } = FieldKind.Raw;

        /// <summary>Unit suffix ("bytes", "entries", null).</summary>
        public string? Unit { get; init; }

        /// <summary>Validation state resolved by the builder.</summary>
        public FieldStatus Status { get; set; } = FieldStatus.NotApplicable;

        /// <summary>Validation detail, e.g. "== 0x43535454".</summary>
        public string? StatusDetail { get; set; }

        /// <summary>Raw bytes as stored in the file (may be null when the field could not be read).</summary>
        public byte[]? Bytes { get; set; }

        // ---- Render helpers -------------------------------------------------

        public bool HasBytes => Bytes != null && Bytes.Length >= Length && Length > 0;

        /// <summary>Unsigned integer value for 1/2/4/8 byte fields.</summary>
        public ulong? Unsigned
        {
            get
            {
                if (!HasBytes) return null;
                var b = Bytes!;
                return Length switch
                {
                    1 => b[0],
                    2 => BitConverter.ToUInt16(b, 0),
                    4 => BitConverter.ToUInt32(b, 0),
                    8 => BitConverter.ToUInt64(b, 0),
                    _ => null,
                };
            }
        }

        /// <summary>Hex representation without prefix, width matching the field size.</summary>
        public string Hex
        {
            get
            {
                if (Bytes == null || Length <= 0) return "—";
                var u = Unsigned;
                if (u.HasValue) return u.Value.ToString("X" + (Length * 2));
                return Convert.ToHexString(Bytes.AsSpan(0, Length));
            }
        }

        /// <summary>Decimal representation for integer fields; "—" otherwise.</summary>
        public string Dec
        {
            get
            {
                var u = Unsigned;
                return u.HasValue ? u.Value.ToString("N0") : "—";
            }
        }

        /// <summary>Best-effort ASCII rendering (printable bytes, '.' otherwise).</summary>
        public string Ascii
        {
            get
            {
                if (Bytes == null || Length <= 0) return string.Empty;
                var sb = new StringBuilder(Length);
                for (int i = 0; i < Length; i++)
                {
                    byte b = Bytes[i];
                    sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
                }
                return sb.ToString();
            }
        }

        /// <summary>True when the ASCII rendering is entirely printable (i.e. a magic like 'CSTT').</summary>
        public bool IsPrintableAscii => HasBytes && Bytes!.Take(Length).All(b => b >= 0x20 && b < 0x7F);

        /// <summary>Status glyph for compact display.</summary>
        public string StatusGlyph => Status switch
        {
            FieldStatus.Ok => "✓",
            FieldStatus.Warning => "!",
            FieldStatus.Error => "✕",
            _ => "",
        };
    }

    /// <summary>
    /// One addressable node in the rpf.cache tree: the file root, a top-level header,
    /// or one subheader.
    /// </summary>
    public sealed class CacheNode
    {
        public CacheNodeKind Kind { get; init; }

        /// <summary>Tree label, e.g. "#1" or "[0]".</summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>Resolved path/name when known; otherwise hash text.</summary>
        public string? Name { get; init; }

        /// <summary>Absolute byte range covered by this node.</summary>
        public long Start { get; init; }
        public long End { get; set; }

        /// <summary>Indices into HSHRFile.MainIndex / HSHRFile.Data.</summary>
        public int TopIndex { get; init; } = -1;
        public int SubIndex { get; init; } = -1;
        public bool IsModified { get; set; }

        public List<CacheNode> Children { get; } = new();

        public long Size => End > Start ? End - Start : 0;
    }

    public enum CacheNodeKind
    {
        Root,
        TopHeader,
        Subheader,
    }
}
