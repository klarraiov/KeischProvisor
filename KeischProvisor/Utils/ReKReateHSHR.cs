using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AuroraLib.Cryptography.Hash;

namespace Respectre.Utils
{
    public class HSHRIndex
    {
        public uint NameHash { get; set; }
        public uint DataOffset { get; set; }
    }

    public class HSHRData
    {
        public uint CSTTMarker { get; set; }
        public uint HeaderSize { get; set; }
        public uint AllDataSize { get; set; }
        public byte[] Header { get; set; }
        public uint SubheadersCount { get; set; }
        public uint IDSTMarker { get; set; }
        public List<HSHRIndex> SubheaderIndex { get; set; } = new List<HSHRIndex>();
        public uint IDENMarker { get; set; }
        public List<byte[]> Subheader { get; set; } = new List<byte[]>();
    }

    public class HSHRFile
    {
        //File System
        public string Name { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }

        // Binary Structure
        public uint Signature { get; set; }
        public uint Version { get; set; }
        public ulong Checksum { get; set; }
        public uint TopHeadersCount { get; set; }
        public List<HSHRIndex> MainIndex { get; set; } = new List<HSHRIndex>();
        public List<HSHRData> Data { get; set; } = new List<HSHRData>();


        // 1013.33 -> VersionMajor: 1013, VersionMinor: 33
        public short VersionMajor { get; set; }
        public short VersionMinor { get; set; }
        public enum HSHRScanStage
        {
            Index,
            Data
        }
        public record struct HSHRScanStatus(
            long Position,
            uint CurrentIndex,
            uint TotalTopHeadersCount,
            HSHRScanStage Stage
        );

        public HSHRFile(string FilePath)
        {
            FileInfo fi = new FileInfo(FilePath);
            this.Name = fi.Name;
            this.FilePath = FilePath;
            this.FileSize = fi.Length;
        }

        public void ScanStructure(Action<HSHRScanStatus>? statusPropagator)
        {

            using var br = new BinaryReader(File.OpenRead(this.FilePath));
            try
            {
                ScanStructure(br, statusPropagator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HSHRFile] ScanStructure Exception: {ex.Message}");
            }
        }

        private void ScanStructure(BinaryReader br, Action<HSHRScanStatus>? statusPropagator)
        {
            // Read Header
            Signature = br.ReadUInt32();
            Version = br.ReadUInt32();
            Checksum = br.ReadUInt64();
            TopHeadersCount = br.ReadUInt32();

            VersionMajor = BitConverter.ToInt16(BitConverter.GetBytes(Version), 2);
            VersionMinor = BitConverter.ToInt16(BitConverter.GetBytes(Version), 0);

            HSHRScanStatus scanStatus = new HSHRScanStatus
            {
                Position = 0,
                CurrentIndex = 0,
                TotalTopHeadersCount = TopHeadersCount,
                Stage = HSHRScanStage.Index
            };

            // Read Main Index
            for (uint i = 0; i < TopHeadersCount; i++)
            {
                scanStatus.Position = br.BaseStream.Position;
                scanStatus.CurrentIndex = i + 1;
                scanStatus.TotalTopHeadersCount = TopHeadersCount;
                scanStatus.Stage = HSHRScanStage.Index;
                if (statusPropagator != null) statusPropagator(scanStatus);
                Debug.WriteLine($"[HSHRFile] Reading at {br.BaseStream.Position}. Index {i + 1}/{TopHeadersCount}");


                HSHRIndex index = new HSHRIndex
                {
                    NameHash = br.ReadUInt32(),
                    DataOffset = br.ReadUInt32()
                };
                MainIndex.Add(index);
            }

            // Read Data
            for (uint i = 0; i < TopHeadersCount; i++)
            {
                scanStatus.Position = br.BaseStream.Position;
                scanStatus.CurrentIndex = i + 1;
                scanStatus.TotalTopHeadersCount = TopHeadersCount;
                scanStatus.Stage = HSHRScanStage.Data;
                if (statusPropagator != null) statusPropagator(scanStatus);
                Debug.WriteLine($"[HSHRFile] Reading at {br.BaseStream.Position}. Data {i + 1}/{TopHeadersCount}");

                var CSTTPosition = br.BaseStream.Position;
                HSHRData data = new HSHRData();
                data.CSTTMarker = br.ReadUInt32();
                data.HeaderSize = br.ReadUInt32();
                data.AllDataSize = br.ReadUInt32();
                data.Header = br.ReadBytes((int)data.HeaderSize);
                data.SubheadersCount = br.ReadUInt32();
                data.IDSTMarker = br.ReadUInt32();

                // Read Subheader Index
                for (uint j = 0; j < data.SubheadersCount; j++)
                {
                    HSHRIndex subIndex = new HSHRIndex
                    {
                        NameHash = br.ReadUInt32(),
                        DataOffset = br.ReadUInt32()
                    };
                    data.SubheaderIndex.Add(subIndex);
                }
                data.IDENMarker = br.ReadUInt32();

                // Read Subheaders
                for (uint j = 0; j < data.SubheadersCount; j++)
                {
                    br.BaseStream.Position = data.SubheaderIndex[(int)j].DataOffset;
                    uint rpfSignature = br.ReadUInt32();
                    uint rpfEntryCount = br.ReadUInt32();
                    uint rpfNamesLength = br.ReadUInt32();
                    uint rpfEncryption = br.ReadUInt32();
                    br.BaseStream.Position -= 16; // Move back to the start of the RPF subheader
                    byte[] subheaderbyte = br.ReadBytes((int)(16 + rpfEntryCount * 16 + rpfNamesLength)); // Read the entire RPF subheader
                    data.Subheader.Add(subheaderbyte);
                }
                Data.Add(data);

                //Header에 패딩이 포함되어 있으므로, 잘라내기
                int indexOfRpfMagic = IndexOfMagic(data.Header);
                br.BaseStream.Position = CSTTPosition + 12 + indexOfRpfMagic + 4;
                uint entryCount = br.ReadUInt32();
                uint namesLength = br.ReadUInt32();
                uint ActualHeaderSize = 16 + entryCount * 16 + namesLength;
                data.Header = data.Header.AsSpan(indexOfRpfMagic, (int)ActualHeaderSize).ToArray();

                br.BaseStream.Position = CSTTPosition + data.AllDataSize + 12; // Engine invariant: CSTT + AllDataSize + 12 -> Next CSTT
            }
        }

        private byte[] BuildSeed(uint v)
        {
            byte b = (byte)(v & 0xFF);
            return new byte[] {
                0x00,
                (byte)((b - 7) & 0xFF),
                (byte)((b - 6) & 0xFF),
                (byte)((b - 5) & 0xFF),
                (byte)((b - 4) & 0xFF),
                (byte)((b - 3) & 0xFF),
                (byte)((b - 2) & 0xFF),
                (byte)((b - 1) & 0xFF),
            };
        }

        private ulong CalculateChecksum(byte[] HSHRBinary)
        {
            if (HSHRBinary == null) throw new ArgumentNullException(nameof(HSHRBinary));
            if (HSHRBinary.Length < 16) throw new ArgumentException("HSHRBinary must be at least 16 bytes long", nameof(HSHRBinary));

            // 0x08 - 0x0F seed
            byte[] rpfcache = HSHRBinary.ToArray();
            byte[] seedBytes = BuildSeed(rpfcache[0x10]);
            ulong seed = BitConverter.ToUInt64(seedBytes);
            Buffer.BlockCopy(seedBytes, 0, rpfcache, 0x08, 8);

            // CityHash64
            CityHash64 cityHash64 = new CityHash64();
            cityHash64.Compute(rpfcache);
            ulong h = cityHash64.Value;

            // Finalizer
            const ulong K_BIAS = 0x651E95C4D06FBFB1UL;
            const ulong KMUL = 0x9DDFEA08EB382D69UL;
            ulong x = h + K_BIAS;
            x ^= seed;
            x *= KMUL;
            x ^= (x >> 47);
            x ^= seed;
            x *= KMUL;
            x ^= (x >> 47);
            x *= KMUL;

            return x;
        }

        private int IndexOfMagic(byte[] bytes)
        {
            byte[] RpfMagic = { (byte)'7', (byte)'F', (byte)'P', (byte)'R' };

            for (int i = 0; i <= bytes.Length - RpfMagic.Length; i++)
            {
                if (bytes[i] == RpfMagic[0] &&
                    bytes[i + 1] == RpfMagic[1] &&
                    bytes[i + 2] == RpfMagic[2] &&
                    bytes[i + 3] == RpfMagic[3])
                {
                    return i;
                }
            }

            return -1;
        }

        //public byte[] ReKReateHSHR()
        //{
        //    const uint MarkerCSTT = 0x43535454;
        //    const uint MarkerIDST = 0x49445354;
        //    const uint MarkerIDEN = 0x4944454E;

        //    using var ms = new MemoryStream();
        //    using var bw = new BinaryWriter(ms);
        //    using var br = new BinaryReader(ms);

        //    // Write Header
        //    bw.Write(Signature);
        //    bw.Write(Version);
        //    bw.Write((ulong)0); // Placeholder for Checksum
        //    bw.Write(TopHeadersCount);

        //    // Write Main Index
        //    uint[] MainIndexDataOffsetCalculated = new uint[TopHeadersCount];
        //    for (int i = 0; i < TopHeadersCount; i++)
        //    {
        //        bw.Write(MainIndex[i].NameHash);
        //        bw.Write((uint)0); // data_offset placeholder
        //    }

        //    // Write Data
        //    // 이미 분석한 HeaderSize, SubheaderCount는 외부에서 수정되었으므로 Header.length와 Subheader.Count를 이용해야 함
        //    for (uint i = 0; i < TopHeadersCount; i++)
        //    {
        //        Debug.WriteLine($"[HSHRFile] Writing Data {i} at {ms.Position}");
        //        HSHRData data = Data[(int)i];
        //        long dataStartPos = ms.Position;

        //        // Write CSTT Marker
        //        bw.Write(MarkerCSTT);
        //        bw.Write((uint)0); // header_size placeholder
        //        bw.Write((uint)0); // all_data_size placeholder

        //        //padding. 16bytes alignment for RPF magic
        //        uint InnateHeaderPadding = (uint)IndexOfMagic(data.Header);
        //        uint FrontPadding = (uint)((16 - (ms.Position + InnateHeaderPadding) % 16) % 16);
        //        Debug.WriteLine(FrontPadding);
        //        bw.Write(new byte[FrontPadding]);
        //        bw.Write(data.Header);
        //        uint currentPosition = (uint)ms.Position;

        //        // postpadding. RpfFile.EntryCount * 2
        //        br.BaseStream.Position = dataStartPos + 12 + InnateHeaderPadding + FrontPadding + 4;
        //        uint rpfEntryCount = br.ReadUInt32(); // read RPF entry count
        //        uint PostPadding = rpfEntryCount * 2;
        //        ms.Position = currentPosition;
        //        bw.Write(new byte[PostPadding]);
        //        Debug.WriteLine(PostPadding);

        //        uint RpfMagicStart = (uint)dataStartPos + 12 + InnateHeaderPadding + FrontPadding;

        //        //update Header Size
        //        uint currentMemoryStreamPosition = (uint)ms.Position;
        //        ms.Position = dataStartPos + 4;
        //        bw.Write(FrontPadding + data.Header.Length + PostPadding);
        //        ms.Position = currentMemoryStreamPosition;

        //        bw.Write(data.Subheader.Count);
        //        bw.Write(MarkerIDST);
        //        // Write Subheader Index
        //        for (int j = 0; j < data.Subheader.Count; j++)
        //        {
        //            bw.Write(data.SubheaderIndex[j].NameHash);
        //            bw.Write((uint)0); // subheader_data_offset placeholder
        //        }
        //        bw.Write(MarkerIDEN);

        //        // Write Subheaders
        //        uint[] SubheaderOffsetCalculated = new uint[data.Subheader.Count];
        //        for (int j = 0; j < data.Subheader.Count; j++)
        //        {
        //            //padding. 16bytes alignment for RPF magic
        //            uint SubheaderPadding = (uint)((16 - (ms.Position % 16)) % 16);
        //            bw.Write(new byte[SubheaderPadding]);

        //            SubheaderOffsetCalculated[j] = (uint)ms.Position;
        //            bw.Write(data.Subheader[j]);
        //            uint currentPosition2 = (uint)ms.Position;

        //            //padding after subheader. just to be safe
        //            br.BaseStream.Position = SubheaderOffsetCalculated[j] + 4;
        //            uint rpfSubheaderEntryCount = br.ReadUInt32();
        //            uint postSubheaderPadding = rpfSubheaderEntryCount * 2;
        //            ms.Position = currentPosition2;
        //            bw.Write(new byte[postSubheaderPadding]);
        //        }

        //        long dataEndPos = ms.Position;

        //        // Update all_data_size
        //        uint allDataSizeCalculated = (uint)(dataEndPos - dataStartPos - 12);
        //        ms.Position = dataStartPos + 8;
        //        bw.Write(allDataSizeCalculated);

        //        // update DataOffset in Subheader Index
        //        long subheaderIndexStartPos = dataStartPos + 12 + data.Header.Length + 8 + (InnateHeaderPadding + FrontPadding + PostPadding);
        //        ms.Position = subheaderIndexStartPos + 4;
        //        for (int j = 0; j < data.Subheader.Count; j++)
        //        {
        //            bw.Write(SubheaderOffsetCalculated[j]);
        //            ms.Position += 4;
        //        }
        //        // Store calculated DataOffset for Main Index update
        //        MainIndexDataOffsetCalculated[i] = RpfMagicStart;


        //        ms.Position = dataEndPos;
        //    }

        //    // Update DataOffset in Main Index
        //    ms.Position = 20;
        //    for (int i = 0; i < TopHeadersCount; i++)
        //    {
        //        ms.Position += 4; // Skip NameHash
        //        bw.Write(MainIndexDataOffsetCalculated[i]);
        //    }


        //    // Calculate and write Checksum
        //    byte[] hshrBinary = ms.ToArray();
        //    ulong checksum = CalculateChecksum(hshrBinary);
        //    Buffer.BlockCopy(BitConverter.GetBytes(checksum), 0, hshrBinary, 8, 8);

        //    return hshrBinary;
        //}



        public byte[] ReKReateHSHR()
        {
            const uint MarkerCSTT = 0x43535454;
            const uint MarkerIDST = 0x49445354;
            const uint MarkerIDEN = 0x4944454E;

            using var MemoryStreamData = new MemoryStream();
            using var BinaryWriter = new BinaryWriter(MemoryStreamData);
            using var BinaryReader = new BinaryReader(MemoryStreamData);

            BinaryWriter.Write(Signature);
            BinaryWriter.Write(Version);
            BinaryWriter.Write((ulong)0); // Placeholder for Checksum
            BinaryWriter.Write(TopHeadersCount);

            //Write Main index
            uint[] MainIndexDataOffsetCalculated = new uint[TopHeadersCount];
            for (int i = 0; i < TopHeadersCount; i++)
            {
                BinaryWriter.Write(MainIndex[i].NameHash);
                BinaryWriter.Write((uint)0); // data_offset placeholder
            }

            //Write Data
            for (int i = 0; i < TopHeadersCount; i++)
            {
                HSHRData data = Data[i];
                long DataStartPosition = MemoryStreamData.Position;

                BinaryWriter.Write(MarkerCSTT);
                BinaryWriter.Write((uint)0); // header_size placeholder
                BinaryWriter.Write((uint)0); // all_data_size placeholder

                // padding for 16bytes alignment
                uint FrontPadding = (uint)((16 - (MemoryStreamData.Position % 16)) % 16);
                BinaryWriter.Write(new byte[FrontPadding]);

                // Write Header
                long MainHeaderPosition = MemoryStreamData.Position;
                MainIndexDataOffsetCalculated[i] = (uint)MemoryStreamData.Position;
                BinaryWriter.Write(data.Header);
                long PostPaddingPosition = MemoryStreamData.Position;

                // post padding for engine invariant
                MemoryStreamData.Position = MainHeaderPosition + 4;
                uint PostPaddingLength = BinaryReader.ReadUInt32() * 2;
                MemoryStreamData.Position = PostPaddingPosition;
                BinaryWriter.Write(new byte[PostPaddingLength]);

                //update Header Size
                long EndofHeaderPosition = MemoryStreamData.Position;
                uint HeaderSizeCalculated = (uint)(EndofHeaderPosition - DataStartPosition - 12);
                MemoryStreamData.Position = DataStartPosition + 4;
                BinaryWriter.Write(HeaderSizeCalculated);
                MemoryStreamData.Position = EndofHeaderPosition;

                // Write Subheader Count and IDST Marker
                BinaryWriter.Write(data.Subheader.Count);
                BinaryWriter.Write(MarkerIDST);


                // Write Subheader Index
                uint[] SubheaderIndexDataOffsetCalculated = new uint[data.Subheader.Count];
                for (int j = 0; j < data.Subheader.Count; j++)
                {
                    BinaryWriter.Write(data.SubheaderIndex[j].NameHash);
                    BinaryWriter.Write((uint)0); // subheader_data_offset placeholder
                }
                BinaryWriter.Write(MarkerIDEN);

                // Write Subheaders
                for (int j = 0; j < data.Subheader.Count; j++)
                {
                    // Post padding for 16bytes alignment
                    uint SubheaderPadding = (uint)((16 - (MemoryStreamData.Position % 16)) % 16);
                    BinaryWriter.Write(new byte[SubheaderPadding]);
                    long SubheaderPosition = MemoryStreamData.Position;

                    SubheaderIndexDataOffsetCalculated[j] = (uint)MemoryStreamData.Position;
                    BinaryWriter.Write(data.Subheader[j]);

                    // post padding for engine invariant
                    long EndofSubheaderPosition = MemoryStreamData.Position;
                    MemoryStreamData.Position = SubheaderPosition + 4;
                    uint SubheaderPostPaddingLength = BinaryReader.ReadUInt32() * 2;
                    MemoryStreamData.Position = EndofSubheaderPosition;
                    BinaryWriter.Write(new byte[SubheaderPostPaddingLength]);
                }

                long DataEndPosition = MemoryStreamData.Position;

                // Update all_data_size
                uint AllDataSizeCalculated = (uint)(DataEndPosition - DataStartPosition - 12);
                MemoryStreamData.Position = DataStartPosition + 8;
                BinaryWriter.Write(AllDataSizeCalculated);

                // update DataOffset in Subheader Index
                long SubheaderIndexStartPosition = DataStartPosition + 4 + 4 + 4 + HeaderSizeCalculated + 4 + 4; // CSTT + HeaderSize + AllDataSize + Header + SubheaderCount + IDST
                MemoryStreamData.Position = SubheaderIndexStartPosition + 4; // skip NameHash
                for (int j = 0; j < data.Subheader.Count; j++)
                {
                    BinaryWriter.Write(SubheaderIndexDataOffsetCalculated[j]);
                    MemoryStreamData.Position += 4;
                }

                MemoryStreamData.Position = DataEndPosition;
            }

            // Update DataOffset in Main Index
            MemoryStreamData.Position = 20;
            for (int i = 0; i < TopHeadersCount; i++)
            {
                MemoryStreamData.Position += 4; // Skip NameHash
                BinaryWriter.Write(MainIndexDataOffsetCalculated[i]);
            }

            // Calculate and write Checksum
            byte[] HSHRBinary = MemoryStreamData.ToArray();
            ulong ChecksumCalculated = CalculateChecksum(HSHRBinary);
            Buffer.BlockCopy(BitConverter.GetBytes(ChecksumCalculated), 0, HSHRBinary, 8, 8);

            return HSHRBinary;
        }
    }
}
