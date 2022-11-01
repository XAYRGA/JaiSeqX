using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;

namespace jaudio
{
    internal class WaveSystem
    {
        private const int WSYS = 0x57535953;
        private const int WINF = 0x57494E46;
        private const int WBCT = 0x57424354;
        public int id;
        public int total_sounds;

        public WSYSScene[] Scenes; 
        public WSYSGroup[] Groups; 


        private void loadWinf(BeBinaryReader rd)
        {
            if (rd.ReadInt32() != WINF)
                throw new Exception("WINF corrupt");
            var count = rd.ReadInt32();
            var ptrs = util.readInt32Array(rd, count);
            Groups = new WSYSGroup[count];
            for (int i=0; i < count; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                Groups[i] = WSYSGroup.CreateFromStream(rd);
            }
        }

        private void loadWbct(BeBinaryReader rd)
        {
            if (rd.ReadInt32() != WBCT)
                throw new Exception("WBCT corrupt");
            rd.ReadInt32(); // Empty?
            var count = rd.ReadInt32();
            var ptrs = util.readInt32Array(rd, count);
            Scenes = new WSYSScene[count];
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                Scenes[i] = WSYSScene.CreateFromStream(rd);
            }
        }

        public static WaveSystem CreateFromStream(BeBinaryReader rd)
        {
            var b = new WaveSystem();
            b.loadFromStream(rd);
            return b;
        }

        private void loadFromStream(BeBinaryReader rd)
        {
            if (rd.ReadInt32() != WSYS)
                throw new InvalidDataException("Couldn't match WSYS header!");
            var size = rd.ReadInt32();
            id = rd.ReadInt32();
            total_sounds = rd.ReadInt32();

            var winfOffset = rd.ReadInt32();
            var wbctOffset = rd.ReadInt32();

            rd.BaseStream.Position = winfOffset;
            loadWinf(rd);

            rd.BaseStream.Position = wbctOffset;
            loadWbct(rd);
        }


        private int writeWinf(BeBinaryWriter wr)
        {
            for (int i=0; i < Groups.Length;i++)
                Groups[i].WriteToStream(wr);


            util.padTo(wr, 32);
            var winfOffs = (int)wr.BaseStream.Position;
            wr.Write(WINF);
            wr.Write(Groups.Length);

            for (int i = 0; i < Groups.Length; i++)
                wr.Write(Groups[i].mBaseAddress);

            return winfOffs;
        }


        private int writeWbct(BeBinaryWriter wr)
        {
            for (int i = 0; i < Scenes.Length; i++)
                Scenes[i].WriteToStream(wr);

            util.padTo(wr, 32);
            var wbctOffs = (int)wr.BaseStream.Position;
            wr.Write(WBCT);
            wr.Write(0);
            wr.Write(Scenes.Length);

            for (int i = 0; i < Scenes.Length; i++)
                wr.Write(Scenes[i].mBaseAddress);

            return wbctOffs;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(WSYS);
            var ret = wr.BaseStream.Position;
            wr.Write(0); // size
            wr.Write(0); // id
            wr.Write(0); // total 
            wr.Write(0); // wbct
            wr.Write(0); // winf

            util.padTo(wr, 32);

            var winfOffs = writeWinf(wr);
            var wbctOffs = writeWbct(wr);
          
    
            var size = (int)wr.BaseStream.Position;

            // Calculate highest waveid.
            var highest = 0;
            for (int i = 0; i < Scenes.Length; i++)
                for (int b = 0; b < Scenes[i].DEFAULT.Length; b++)
                    if (Scenes[i].DEFAULT[b].WaveID > highest)
                        highest = Scenes[i].DEFAULT[b].WaveID;

            wr.BaseStream.Position = ret;
            wr.Write(size);
            wr.Write(id);
            wr.Write(highest);
            wr.Write(winfOffs);
            wr.Write(wbctOffs);
   
            wr.BaseStream.Position = size;

            util.padTo(wr, 32);
        }
    }

    public class WSYSScene : JAudioSerializable
    {

        private const int SCNE = 0x53434E45;

        private const int C_DF = 0x432D4446;
        private const int C_EX = 0x432D4558;
        private const int C_ST = 0x432D5354;

        public WSYSWaveID[] DEFAULT;
        public WSYSWaveID[] EXTENDED;
        public WSYSWaveID[] STATIC;

        private WSYSWaveID[] loadContainer(BeBinaryReader rd, int type)
        {
            var inType = rd.ReadInt32();
            if (inType != type)
                throw new Exception($"Unexpected section type {type:X} != {inType:X}");
            var count = rd.ReadInt32();
            var waves = new WSYSWaveID[count];
            var offsets = util.readInt32Array(rd, count);
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = offsets[i];
                waves[i] = WSYSWaveID.CreateFromStream(rd);
            }
            return waves;
        }

        private int writeContainer(BeBinaryWriter wr, int type, WSYSWaveID[] outw)
        {

            for (int i = 0; i < outw.Length; i++)
                outw[i].WriteToStream(wr);
            util.padTo(wr, 32);
            var pos = (int) wr.BaseStream.Position;
            wr.Write(type);
            wr.Write(outw.Length);
            for (int i = 0; i < outw.Length; i++)
                wr.Write(outw[i].mBaseAddress);

            return pos; 
        }

        public static WSYSScene CreateFromStream(BeBinaryReader rd)
        {
            var b = new WSYSScene();
            b.loadFromStream(rd);
            return b;
        }


        private void loadFromStream(BeBinaryReader rd)
        {
            if (rd.ReadInt32() != SCNE)
                throw new Exception("SCNE corrupt");
            rd.ReadUInt64(); // Padding? Something???? Always zero.
            var cdfOffset = rd.ReadInt32();
            var cexOffset = rd.ReadInt32();
            var cstOffset = rd.ReadInt32();

            rd.BaseStream.Position = cdfOffset;
            DEFAULT = loadContainer(rd, C_DF);
            rd.BaseStream.Position = cexOffset;
            EXTENDED = loadContainer(rd, C_EX);
            rd.BaseStream.Position = cstOffset;
            STATIC = loadContainer(rd, C_ST);
        }

        public override void WriteToStream(BeBinaryWriter wr)
        {
            var cdfOffset = writeContainer(wr, C_DF, DEFAULT);
            var cexOffset = writeContainer(wr, C_EX, EXTENDED);
            var cstOffset = writeContainer(wr, C_ST, STATIC);


            util.padTo(wr, 32);
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.Write(SCNE);
            wr.Write(0L);
            wr.Write(cdfOffset);
            wr.Write(cexOffset);
            wr.Write(cstOffset);
        }
    }

    public class WSYSGroup : JAudioSerializable
    {
        public string awPath;
        public WSYSWave[] waves;

        public static WSYSGroup CreateFromStream(BeBinaryReader rd)
        {
            var b = new WSYSGroup();
            b.loadFromStream(rd);
            return b;
        }

        private void loadFromStream(BeBinaryReader rd)
        {
            awPath = "";
            var stringBuff = rd.ReadBytes(0x70);
            for (int i = 0; i < 0x70; i++)
                if (stringBuff[i] != 0)
                    awPath += (char)stringBuff[i];
                else
                    break;
            
            var count = rd.ReadInt32();
            var ptrs = util.readInt32Array(rd, count);
            waves = new WSYSWave[ptrs.Length];
            for (int i = 0; i < ptrs.Length; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                waves[i] = WSYSWave.CreateFromStream(rd);
            }
        }

        public override void WriteToStream(BeBinaryWriter wr)
        { 
            // We write the waves first so their offsets are allocated
            for (int i = 0; i < waves.Length; i++)
                waves[i].WriteToStream(wr);

            util.padTo(wr, 32);

            mBaseAddress = (int)wr.BaseStream.Position;
            byte[] buff = new byte[0x70];
            for (int i=0; i < awPath.Length; i++)
                buff[i] = (byte)awPath[i];
            wr.Write(buff);
            wr.Write(waves.Length);

            for (int i = 0; i < waves.Length; i++)
                wr.Write(waves[i].mBaseAddress);
        }
    }


    public class WSYSWaveID : JAudioSerializable
    {
        public short GroupID; 
        public short WaveID;
        
        public void loadFromStream(BeBinaryReader rd)
        {
            GroupID = rd.ReadInt16();
            WaveID = rd.ReadInt16();
            rd.ReadInt32(); // CCCCCCCC
            rd.ReadInt32(); // FFFFFFFF
        }
        public static WSYSWaveID CreateFromStream(BeBinaryReader rd)
        {
            var b = new WSYSWaveID();
            b.loadFromStream(rd);
            return b;
        }

        public override void WriteToStream(BeBinaryWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.Write(GroupID);
            wr.Write(WaveID);
            wr.Write(new byte[0x2C]); // Empty?
            wr.Write(0xCCCCCCCC); // Uninitialized stack
            wr.Write(0xFFFFFFFF); // nice
        }
    }
    

    public class WSYSWave : JAudioSerializable
    {
        public byte format;
        public byte key;
        public float sampleRate;
        public int sampleCount;

        public int awOffset;
        public int awLength;

        public bool loop;
        public int loop_start;
        public int loop_end;

        public short last;
        public short penult;


        public void loadFromStream(BeBinaryReader rd)
        {
            rd.ReadByte(); // Empty.
            format = rd.ReadByte();
            key = rd.ReadByte();
            rd.ReadByte(); // empty. 
            sampleRate = rd.ReadSingle();
            awOffset = rd.ReadInt32();
            awLength = rd.ReadInt32();
            loop = rd.ReadUInt32() == 0xFFFFFFFF;
            loop_start = rd.ReadInt32();
            loop_end = rd.ReadInt32();
            sampleCount = rd.ReadInt32();
            last = rd.ReadInt16();
            penult = rd.ReadInt16();
           
            rd.ReadInt32(); // Zero.
            rd.ReadInt32(); // 0xCCCCCCCC Uninitialized stack
        }

        public static WSYSWave CreateFromStream(BeBinaryReader rd)
        {
            var b = new WSYSWave();
            b.loadFromStream(rd);
            return b;
        }

        public override void WriteToStream(BeBinaryWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.Write((byte)0xCC);
            wr.Write(format);
            wr.Write(key);
            wr.Write((byte)0);
            wr.Write(sampleRate);
            wr.Write(awOffset);
            wr.Write(awLength);
            wr.Write(loop ? 0xFFFFFFFF : 0);
            wr.Write(loop_start);
            wr.Write(loop_end);
            wr.Write(sampleCount);
            wr.Write(last);
            wr.Write(penult);
            wr.Write(0);
            wr.Write(0xCCCCCCCC);
        }
    }
}

