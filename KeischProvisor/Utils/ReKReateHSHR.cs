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

        public static Dictionary<uint, string> namehashPairs = new Dictionary<uint, string>
        {
            {1094518106, "common.rpf"},
            {601479070, "x64a.rpf"},
            {2195688322, "x64b.rpf"},
            {3421233537, "x64c.rpf"},
            {1370153525, "x64d.rpf"},
            {3198892658, "x64e.rpf"},
            {221396530, "x64f.rpf"},
            {902986816, "x64g.rpf"},
            {1877458086, "x64h.rpf"},
            {2089581604, "x64i.rpf"},
            {2303042894, "x64j.rpf"},
            {2345688009, "x64k.rpf"},
            {1679543726, "x64l.rpf"},
            {445686000, "x64m.rpf"},
            {814393134, "x64n.rpf"},
            {1038456369, "x64o.rpf"},
            {2242638391, "x64p.rpf"},
            {2314325720, "x64q.rpf"},
            {457568906, "x64r.rpf"},
            {214566609, "x64s.rpf"},
            {975824587, "x64t.rpf"},
            {3041989297, "x64u.rpf"},
            {2063085026, "x64v.rpf"},
            {1696346790, "x64w.rpf"},
            {179905116, "update/update.rpf"},
            {3183318290, "update/update2.rpf"},
            {1548568639, "x64/audio/audio_rel.rpf"},
            {2520465068, "x64/audio/occlusion.rpf"},
            {2947691198, "x64/audio/sfx/ANIMALS.rpf"},
            {2951507943, "x64/audio/sfx/ANIMALS_FAR.rpf"},
            {329683295, "x64/audio/sfx/ANIMALS_NEAR.rpf"},
            {1016140578, "x64/audio/sfx/CUTSCENE_MASTERED_ONLY.rpf"},
            {1182037611, "x64/audio/sfx/DLC_GTAO.rpf"},
            {1726354385, "x64/audio/sfx/INTERACTIVE_MUSIC.rpf"},
            {3202626340, "x64/audio/sfx/ONESHOT_AMBIENCE.rpf"},
            {2279488664, "x64/audio/sfx/PAIN.rpf"},
            {2315136875, "x64/audio/sfx/POLICE_SCANNER.rpf"},
            {2749874294, "x64/audio/sfx/PROLOGUE.rpf"},
            {2628790602, "x64/audio/sfx/RADIO_01_CLASS_ROCK.rpf"},
            {1968008831, "x64/audio/sfx/RADIO_02_POP.rpf"},
            {1907719024, "x64/audio/sfx/RADIO_03_HIPHOP_NEW.rpf"},
            {2716005716, "x64/audio/sfx/RADIO_04_PUNK.rpf"},
            {402356163, "x64/audio/sfx/RADIO_05_TALK_01.rpf"},
            {1731527840, "x64/audio/sfx/RADIO_06_COUNTRY.rpf"},
            {4033359556, "x64/audio/sfx/RADIO_07_DANCE_01.rpf"},
            {1671330257, "x64/audio/sfx/RADIO_08_MEXICAN.rpf"},
            {3063475839, "x64/audio/sfx/RADIO_09_HIPHOP_OLD.rpf"},
            {3831421761, "x64/audio/sfx/RADIO_11_TALK_02.rpf"},
            {2255796457, "x64/audio/sfx/RADIO_12_REGGAE.rpf"},
            {3715529878, "x64/audio/sfx/RADIO_13_JAZZ.rpf"},
            {4240736810, "x64/audio/sfx/RADIO_14_DANCE_02.rpf"},
            {3317387673, "x64/audio/sfx/RADIO_15_MOTOWN.rpf"},
            {3921387594, "x64/audio/sfx/RADIO_16_SILVERLAKE.rpf"},
            {1220541098, "x64/audio/sfx/RADIO_17_FUNK.rpf"},
            {1958056106, "x64/audio/sfx/RADIO_18_90S_ROCK.rpf"},
            {91879205, "x64/audio/sfx/RADIO_ADVERTS.rpf"},
            {282543935, "x64/audio/sfx/RADIO_NEWS.rpf"},
            {2535662910, "x64/audio/sfx/RESIDENT.rpf"},
            {3404921760, "x64/audio/sfx/SCRIPT.rpf"},
            {1843971178, "x64/audio/sfx/SS_AC.rpf"},
            {1585946477, "x64/audio/sfx/SS_DE.rpf"},
            {3228627738, "x64/audio/sfx/SS_FF.rpf"},
            {1231368532, "x64/audio/sfx/SS_GM.rpf"},
            {137226421, "x64/audio/sfx/SS_NP.rpf"},
            {1860643759, "x64/audio/sfx/SS_QR.rpf"},
            {2512337277, "x64/audio/sfx/SS_ST.rpf"},
            {2855644158, "x64/audio/sfx/SS_UZ.rpf"},
            {700602439, "x64/audio/sfx/STREAMED_AMBIENCE.rpf"},
            {1159573775, "x64/audio/sfx/STREAMED_VEHICLES.rpf"},
            {1573197398, "x64/audio/sfx/STREAMED_VEHICLES_GRANULAR.rpf"},
            {3521592818, "x64/audio/sfx/STREAMED_VEHICLES_GRANULAR_NPC.rpf"},
            {2587139650, "x64/audio/sfx/STREAMED_VEHICLES_LOW_LATENCY.rpf"},
            {3960129415, "x64/audio/sfx/STREAMS.rpf"},
            {2144676109, "x64/audio/sfx/S_FULL_AMB_F.rpf"},
            {3351792429, "x64/audio/sfx/S_FULL_AMB_M.rpf"},
            {4053703657, "x64/audio/sfx/S_FULL_GAN.rpf"},
            {4235226870, "x64/audio/sfx/S_FULL_SER.rpf"},
            {3537953157, "x64/audio/sfx/S_MINI_AMB.rpf"},
            {3183039620, "x64/audio/sfx/S_MINI_GAN.rpf"},
            {1050927715, "x64/audio/sfx/S_MINI_SER.rpf"},
            {3116572378, "x64/audio/sfx/S_MISC.rpf"},
            {1022127128, "x64/audio/sfx/WEAPONS_PLAYER.rpf"},
            {2721356894, "update/x64/dlcpacks/mp2023_01/dlc.rpf"},
            {3211914171, "update/x64/dlcpacks/mp2023_01_g9ec/dlc.rpf"},
            {1237371662, "update/x64/dlcpacks/mp2023_02/dlc.rpf"},
            {2980348889, "update/x64/dlcpacks/mp2023_02_g9ec/dlc.rpf"},
            {2981017901, "update/x64/dlcpacks/mp2024_01/dlc.rpf"},
            {2586626485, "update/x64/dlcpacks/mp2024_01_g9ec/dlc.rpf"},
            {32872603, "update/x64/dlcpacks/mp2024_02/dlc.rpf"},
            {469140369, "update/x64/dlcpacks/mp2024_02_G9EC/dlc.rpf"},
            {2669714237, "update/x64/dlcpacks/mp2025_01/dlc.rpf"},
            {1537777326, "update/x64/dlcpacks/mp2025_01_G9EC/dlc.rpf"},
            {3346517050, "update/x64/dlcpacks/mp2025_02/dlc.rpf"},
            {3061963327, "update/x64/dlcpacks/mp2025_02_g9ec/dlc.rpf"},
            {1672348209, "update/x64/dlcpacks/mpairraces/dlc.rpf"},
            {251860201, "update/x64/dlcpacks/mpapartment/dlc.rpf"},
            {723647593, "update/x64/dlcpacks/mpassault/dlc.rpf"},
            {1751390105, "update/x64/dlcpacks/mpbattle/dlc.rpf"},
            {1938324268, "update/x64/dlcpacks/mpbattle/dlc1.rpf"},
            {2132449846, "update/x64/dlcpacks/mpbiker/dlc.rpf"},
            {2008070623, "update/x64/dlcpacks/mpchristmas2/dlc.rpf"},
            {4173325385, "update/x64/dlcpacks/mpchristmas2017/dlc.rpf"},
            {2750379856, "update/x64/dlcpacks/mpchristmas2018/dlc.rpf"},
            {4115046898, "update/x64/dlcpacks/mpchristmas3/dlc.rpf"},
            {3407584235, "update/x64/dlcpacks/mpchristmas3_g9ec/dlc.rpf"},
            {3941437756, "update/x64/dlcpacks/mpexecutive/dlc.rpf"},
            {946941868, "update/x64/dlcpacks/mpg9ec/dlc.rpf"},
            {2663798158, "update/x64/dlcpacks/mpgunrunning/dlc.rpf"},
            {517842482, "update/x64/dlcpacks/mphalloween/dlc.rpf"},
            {3007321265, "update/x64/dlcpacks/mpheist/dlc.rpf"},
            {2939867158, "update/x64/dlcpacks/mpheist3/dlc.rpf"},
            {3032957969, "update/x64/dlcpacks/mpheist4/dlc.rpf"},
            {2105410797, "update/x64/dlcpacks/mpheist4/dlc1.rpf"},
            {12495402, "update/x64/dlcpacks/mpheist4/dlc2.rpf"},
            {2780400792, "update/x64/dlcpacks/mpimportexport/dlc.rpf"},
            {1228464043, "update/x64/dlcpacks/mpjanuary2016/dlc.rpf"},
            {973384779, "update/x64/dlcpacks/mplowrider/dlc.rpf"},
            {2829481134, "update/x64/dlcpacks/mplowrider2/dlc.rpf"},
            {3499644936, "update/x64/dlcpacks/mpluxe/dlc.rpf"},
            {1722478352, "update/x64/dlcpacks/mpluxe2/dlc.rpf"},
            {1466316336, "update/x64/dlcpacks/mppatchesng/dlc.rpf"},
            {3511974331, "update/x64/dlcpacks/mpreplay/dlc.rpf"},
            {1334841096, "update/x64/dlcpacks/mpsecurity/dlc.rpf"},
            {536976874, "update/x64/dlcpacks/mpsecurity/dlc1.rpf"},
            {2286742327, "update/x64/dlcpacks/mpsmuggler/dlc.rpf"},
            {881198735, "update/x64/dlcpacks/mpspecialraces/dlc.rpf"},
            {3574513774, "update/x64/dlcpacks/mpstunt/dlc.rpf"},
            {3630227002, "update/x64/dlcpacks/mpsum/dlc.rpf"},
            {3045251411, "update/x64/dlcpacks/mpsum2/dlc.rpf"},
            {2007662829, "update/x64/dlcpacks/mpSum2_G9EC/dlc.rpf"},
            {2150219453, "update/x64/dlcpacks/mptuner/dlc.rpf"},
            {562314142, "update/x64/dlcpacks/mptuner/dlc1.rpf"},
            {2348311832, "update/x64/dlcpacks/mpvalentines2/dlc.rpf"},
            {3959824251, "update/x64/dlcpacks/mpvinewood/dlc.rpf"},
            {3802105443, "update/x64/dlcpacks/mpxmas_604490/dlc.rpf"},
            {1032462538, "update/x64/dlcpacks/patch2023_01/dlc.rpf"},
            {3180420675, "update/x64/dlcpacks/patch2023_01_g9ec/dlc.rpf"},
            {3759105479, "update/x64/dlcpacks/patch2023_02/dlc.rpf"},
            {3712621519, "update/x64/dlcpacks/patch2024_01/dlc.rpf"},
            {3855963664, "update/x64/dlcpacks/patch2024_01_g9ec/dlc.rpf"},
            {2524525731, "update/x64/dlcpacks/patch2024_02/dlc.rpf"},
            {1082198401, "update/x64/dlcpacks/patch2025_01/dlc.rpf"},
            {854664330, "update/x64/dlcpacks/patch2025_02/dlc.rpf"},
            {4064260007, "update/x64/dlcpacks/patch2025_02_g9ec/dlc.rpf"},
            {2770508567, "update/x64/dlcpacks/patchday10ng/dlc.rpf"},
            {3076730025, "update/x64/dlcpacks/patchday11ng/dlc.rpf"},
            {4226228674, "update/x64/dlcpacks/patchday12ng/dlc.rpf"},
            {1685516714, "update/x64/dlcpacks/patchday13ng/dlc.rpf"},
            {691628848, "update/x64/dlcpacks/patchday14ng/dlc.rpf"},
            {1691007810, "update/x64/dlcpacks/patchday15ng/dlc.rpf"},
            {1545061340, "update/x64/dlcpacks/patchday16ng/dlc.rpf"},
            {3173028122, "update/x64/dlcpacks/patchday17ng/dlc.rpf"},
            {1562658998, "update/x64/dlcpacks/patchday18ng/dlc.rpf"},
            {449174673, "update/x64/dlcpacks/patchday19ng/dlc.rpf"},
            {2253096243, "update/x64/dlcpacks/patchday1ng/dlc.rpf"},
            {1628314094, "update/x64/dlcpacks/patchday20ng/dlc.rpf"},
            {3753907893, "update/x64/dlcpacks/patchday21ng/dlc.rpf"},
            {2281304531, "update/x64/dlcpacks/patchday22ng/dlc.rpf"},
            {2544901939, "update/x64/dlcpacks/patchday23ng/dlc.rpf"},
            {3600655870, "update/x64/dlcpacks/patchday24ng/dlc.rpf"},
            {3383911529, "update/x64/dlcpacks/patchday25ng/dlc.rpf"},
            {261054934, "update/x64/dlcpacks/patchday26ng/dlc.rpf"},
            {1027869455, "update/x64/dlcpacks/patchday27g9ecng/dlc.rpf"},
            {1541561542, "update/x64/dlcpacks/patchday27ng/dlc.rpf"},
            {531911020, "update/x64/dlcpacks/patchday28g9ecng/dlc.rpf"},
            {2107417334, "update/x64/dlcpacks/patchday28ng/dlc.rpf"},
            {3168047264, "update/x64/dlcpacks/patchday2bng/dlc.rpf"},
            {1291184191, "update/x64/dlcpacks/patchday2ng/dlc.rpf"},
            {3187633461, "update/x64/dlcpacks/patchday3ng/dlc.rpf"},
            {3785763136, "update/x64/dlcpacks/patchday4ng/dlc.rpf"},
            {948741674, "update/x64/dlcpacks/patchday5ng/dlc.rpf"},
            {109467700, "update/x64/dlcpacks/patchday6ng/dlc.rpf"},
            {3031518078, "update/x64/dlcpacks/patchday7ng/dlc.rpf"},
            {602440307, "update/x64/dlcpacks/patchday8ng/dlc.rpf"},
            {4294636893, "update/x64/dlcpacks/patchday9ng/dlc.rpf"},
            {33853162, "update/x64/dlcpacks/patchdayg9ecng/dlc.rpf"},

        };

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
