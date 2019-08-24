using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO; 

namespace JaiSeqX.JAI.Types.WSYS
{
    /* 
        Structure of a WSYS
            ENDIAN BIG;
            int32 'WSYS' = 0x57535953; 
            int32 size
            int32 id
            int32 padding
            int32 WaveInfo offsets
            int32 WaveBaseControlTable offsets;

        Structure of WaveInfo 
            int32 'WINF' = ? 
            int32 padding 
            int32 count
                int32xcount WaveGroupOffset

        Structure of WaveBaseControlTable
            int32 'WBCT' = ? 
            int32 padding 
            int32 count
                int32xcount WaveBaseControlTableOffset

        Structure of WaveGroup
            string (0x70) AW filename \xff 
            int32 WaveInfocount
                int32 * WaveInfoCount WaveSceneOffset

        // The two below are completely useless in terms of function. I think. 
        Structure of WaveScene
            int32 'SCNE' = 0x53434E45;

        

    */
    public class WaveSystem
    {
        public int id;
        private int size;
        public WSYSWave[] waves;
        public WSYSGroup[] groups;

        private const int WSYS = 0x57535953;
        private const int SCNE = 0x53434E45;
        private const int C_DF = 0x432D4446;

        private long BaseAddress;

        private int current_header = 0; // utility
        private int back = 0; // utility

        public void LoadWSYS(BeBinaryReader WSYSReader)
        {

            BaseAddress = WSYSReader.BaseStream.Position;
            waves = new WSYSWave[32768]; // TEMPORARY. Fix WSYS Loading!
            current_header = WSYSReader.ReadInt32();
            if (current_header != WSYS)
            {
                Console.WriteLine("Error: Section base at {0} is not WSYS, is instead {1:X}", BaseAddress, current_header);
                return;
            }

            size = WSYSReader.ReadInt32();

            id = WSYSReader.ReadInt32();

            WSYSReader.ReadUInt32(); // 4 bytes, not used. .. I think

            //  A little messy, but both of these need to be loaded before we can continue. 
            var winfo_offset = WSYSReader.ReadInt32(); // relative offset to wave info offset pointer table. 
            var wbct_offset = WSYSReader.ReadInt32(); // relative offset to wave info offset pointer table. 

            int[] winfOffsets;
            int[] wbctOffsets;
            {
                // Load WINF offsets.     
                WSYSReader.BaseStream.Position = BaseAddress + winfo_offset;
                current_header = WSYSReader.ReadInt32(); // Should be WINF
                winfOffsets = new int[WSYSReader.ReadInt32()];  // 4 bytes count

                for (int i = 0; i < winfOffsets.Length; i++)
                {
                    winfOffsets[i] = WSYSReader.ReadInt32();  // Int32's following the length. 
                }

                // Load WBCT data

                WSYSReader.BaseStream.Position = BaseAddress + wbct_offset;
                current_header = WSYSReader.ReadInt32(); // Should be WBCT
                WSYSReader.ReadUInt32(); // 4 bytes unused?
                wbctOffsets = new int[WSYSReader.ReadInt32()]; // 4 bytes count

                for (int i = 0; i < wbctOffsets.Length; i++)
                {
                    wbctOffsets[i] = WSYSReader.ReadInt32();  // Int32's following the length. 
                }
            }

            groups = new WSYSGroup[winfOffsets.Length];
            for (int i=0; i < winfOffsets.Length;i++)
            {

                /* This is loading the data for a WINF */ 
                WSYSReader.BaseStream.Position = BaseAddress + winfOffsets[i];

                var Group = new WSYSGroup();
                Group.path = Helpers.readArchiveName(WSYSReader);


                var fobj = File.OpenRead("./Banks/" + Group.path); // Open the .AW file  (AW contains only ADPCM data, nothing else.)
                var fobj_reader = new BeBinaryReader(fobj); // Create a reader for it. 
             

                int waveInfoCounts = WSYSReader.ReadInt32(); // 4 byte count 
                int[] info_offsets = new int[waveInfoCounts];
                for (int q = 0; q < waveInfoCounts; q++)
                {
                    info_offsets[q] = WSYSReader.ReadInt32();
                }
                Group.IDMap = new int[UInt16.MaxValue]; // We have to initialize the containers for the wave information 
                Group.Waves = new WSYSWave[waveInfoCounts];


                /* Since the count should be equal, we're loading the info for the WBCT in he re as well */
                WSYSReader.BaseStream.Position = BaseAddress + wbctOffsets[i];
                // The first several bytes of the WBCT are uselss, a WBCT points directly to a SCNE. 
                current_header = WSYSReader.ReadInt32(); // This should be SCNE. 
                WSYSReader.ReadUInt64(); // The next 8 bytes are useless. 
                var cdf_offset = WSYSReader.ReadInt32(); // However, the next 4 contain the pointer to c_DF  relative to our base. 
                WSYSReader.BaseStream.Position = BaseAddress + cdf_offset;

                current_header = WSYSReader.ReadInt32(); // Should be C_DF.
                int waveid_count = WSYSReader.ReadInt32(); // Count of our WAVE ID
                int[] waveid_offsets = new int[waveid_count]; // Be ready to store them
                for (int q=0; q < waveid_count; q++)
                {
                    waveid_offsets[q] = WSYSReader.ReadInt32(); // Read each waveid 
                }

               

                // Finally, we're going to get our wave data. 

                for (int wav=0;wav < waveInfoCounts; wav++)
                {
                    var o_Wave = new WSYSWave();
                    WSYSReader.BaseStream.Position = BaseAddress + waveid_offsets[wav];
                    var aw_id = WSYSReader.ReadInt16(); // Strangely enough, it has an AW_ID here. This tells which file it sits in?  I guess they're normally loaded separately. 
                    o_Wave.id = WSYSReader.ReadInt16(); // This is the waveid for this wave, it's normally globally unique, but some games hot-load banks. 


                    WSYSReader.BaseStream.Position = BaseAddress + info_offsets[wav]; // Seek to the offset of the actual wave parameters. 

                    WSYSReader.ReadByte(); // I have no clue what the first byte does. 
                    o_Wave.format = WSYSReader.ReadByte(); // Tells what format it's in, usually type 5 AFC (ADPCM) 
                    o_Wave.key = WSYSReader.ReadByte(); // Tells the base key for this wave (0-127 usually) 
                    WSYSReader.ReadByte(); // I have no clue what this byte does. 
                    //var srate = WSYSReader.ReadBytes(4);

                    o_Wave.sampleRate = WSYSReader.ReadSingle();

                    /*
                     * oh. its a float.
                     * oops
                    if (o_Wave.format == 5)
                    {
                        o_Wave.sampleRate = 32000; /// ????
                    }

                    if (o_Wave.sampleRate == 5666) // What the actual fuck. Broken  value, can't figure out why. 
                    {
                        o_Wave.sampleRate = 44100; // I guess set the srate to 44100? 
                    }
                    */

                    o_Wave.w_start = WSYSReader.ReadUInt32(); // 4 byte start in AW
                    o_Wave.w_size = WSYSReader.ReadUInt32(); // 4 byte size in AW 
                    var b = WSYSReader.ReadUInt32();
                    o_Wave.loop = b==UInt32.MaxValue ? true : false; // Weird looping flag?
                    o_Wave.loop_start = (int)Math.Floor(((WSYSReader.ReadInt32() / 8f) ) * 16f) ;
                    o_Wave.loop_end = (int)Math.Floor(((WSYSReader.ReadInt32()/8f) )  * 16f) ;
             
                    o_Wave.sampleCount = WSYSReader.ReadInt32(); // 4 byte sample cont

                   // Console.WriteLine("L {0:X} (0x{1:X}), LS {2:X} , LE {3:X}, SC {4:X} SZ {5:X}", o_Wave.loop, b, o_Wave.loop_start, o_Wave.loop_end,o_Wave.sampleCount,o_Wave.w_size);
                    //Console.ReadLine();
                    var name = string.Format("0x{0:X}.wav", o_Wave.id);
                    var name2 = string.Format("0x{0:X}.par", o_Wave.id);
                    if (!Directory.Exists("./WSYS_CACHE"))
                    {
                        Directory.CreateDirectory("./WSYS_CACHE");

                    }
                    if (!Directory.Exists("./WSYS_CACHE/AW_" + Group.path))
                    {
                        Directory.CreateDirectory("./WSYS_CACHE/AW_" + Group.path);
                    }

                    o_Wave.pcmpath = "./WSYS_CACHE/AW_" + Group.path + "/" + name;

                    Group.Waves[wav] = o_Wave; // We're done with just about everything except the PCM data now (ADPCM / AFC conversion) 
                    Group.IDMap[o_Wave.id] = wav;
                    waves[o_Wave.id] = o_Wave; // TEMPORARY, FIX WSYS LOADING!

                    fobj_reader.BaseStream.Position = o_Wave.w_start;
                    var adpcm = fobj_reader.ReadBytes((int)o_Wave.w_size);

                    if (!File.Exists(o_Wave.pcmpath))
                    {
                        Helpers.AFCtoPCM16(adpcm, o_Wave.sampleRate, (int)o_Wave.w_size,o_Wave.format, o_Wave.pcmpath);
                    }


                }

            }


              






        }
    }
}
