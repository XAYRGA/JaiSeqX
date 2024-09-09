using jaudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.bananapeel;
using System.Runtime.InteropServices;
using System.Reflection.Metadata.Ecma335;

namespace JaiSeqX
{
    internal static class WaveManager
    {
        private static Dictionary<int, JWaveSystem> WaveSystems = new Dictionary<int, JWaveSystem>();
        private static Dictionary<int, Dictionary<int, JWaveSystem.Wave>> Waves = new Dictionary<int, Dictionary<int, JWaveSystem.Wave>>();
        private static Dictionary<int, Dictionary<int, NativeWaveInfo>> WaveBuffers = new Dictionary<int, Dictionary<int, NativeWaveInfo>>();

        public class NativeWaveInfo
        {
        
            public IntPtr Address;
            public JWaveSystem.Wave Wave;
            public int Length;

            public NativeWaveInfo(JWaveSystem.Wave waveInfo, byte[] native)
            {
                Address = Marshal.AllocHGlobal(native.Length);
                Wave = waveInfo;
                Length = native.Length;
            }

            ~NativeWaveInfo() 
            { 
                Marshal.FreeHGlobal(Address);
            }
        }

        public static void AddWaveBank(JWaveSystem waveSystem, int id = -1)
        {
            var rid = id == -1 ? waveSystem.ID : id;
            WaveSystems[rid] = waveSystem;
            Waves[rid] = waveSystem.Waves;
            WaveBuffers[rid] = new Dictionary<int, NativeWaveInfo>();
        }

        public static byte[] GetWaveRAW(int bankID, int waveID)
        {
            if (Waves.ContainsKey(bankID) && Waves[bankID].ContainsKey(waveID))
                return Waves[bankID][waveID].CurrentBuffer;
            return null;
        }

        public unsafe static NativeWaveInfo GetWaveNative(int bankID, int waveID)
        {
            // Check to see if buffer is cached first.
            if (WaveBuffers.ContainsKey(bankID) && WaveBuffers[bankID].ContainsKey(waveID))
                return WaveBuffers[bankID][waveID];

            // Check for wsys buffer.
            JWaveSystem.Wave wave = null;
            if (Waves.ContainsKey(bankID) && Waves[bankID].ContainsKey(waveID))
                wave = Waves[bankID][waveID];

            if (wave == null)
                return null;

            var nativeBuffer = GenerateNativeBuffer(wave);
            WaveBuffers[bankID][waveID] = nativeBuffer;
            return nativeBuffer;
        }


        public static void RemoveWaveBank(int id)
        {
            WaveSystems.Remove(id);
            Waves.Remove(id);
            WaveBuffers.Remove(id);
        }

        public static void RemoveWaveBank(JWaveSystem waveSystem)
        {
            WaveSystems.Remove(waveSystem.ID);
            Waves.Remove(waveSystem.ID);
            WaveBuffers.Remove(waveSystem.ID);
        }



        private static byte[] wavhead = new byte[44] {
                0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,  0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
                0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,  0x00, 0x00, 0x00, 0x00
        };

        private static NativeWaveInfo GenerateNativeBuffer(JWaveSystem.Wave wave)
        {
            var buffer = wave.CurrentBuffer;
            byte[] native = null;

            switch (wave.Format)
            {
                case JWaveSystem.Wave.EWaveFormat.ADPCM4:
                    native = mux.PCM16ShortToByte( mux.ADPCM4TOPCM16(buffer));
                    break;
                case JWaveSystem.Wave.EWaveFormat.ADPCM2:
                    native = mux.PCM16ShortToByte( mux.ADPCM2TOPCM16(buffer));
                    break;
                case JWaveSystem.Wave.EWaveFormat.PCM8:
                    native = mux.PCM16ShortToByte(mux.PCM8216(buffer));
                    break;
                case JWaveSystem.Wave.EWaveFormat.PCM16: 
                    native = mux.PCM16ShortToByte(mux.PCM16BYTESWAP( mux.PCM16ByteToShort( buffer )));
                    break;
            }

            if (native == null)
                throw new Exception("Didn't generate a buffer for sound");

            var MS = new MemoryStream();
            var beW = new BinaryWriter(MS);
            var osz = native.Length;
            var oszt = osz + 8;
            MS.Write(wavhead, 0, wavhead.Length);
            beW.BaseStream.Position = 4;
            beW.Write(oszt);
            beW.BaseStream.Position = 24;
            beW.Write((int)wave.SampleRate);
            beW.Write((int)wave.SampleRate);
            beW.BaseStream.Position = 40;
            beW.Write((int)osz);
            beW.Write(native, 0, buffer.Length);
            beW.Flush();
            var fileBuffer = MS.ToArray();
            beW.Close();
            MS.Close();

            return new NativeWaveInfo(wave, fileBuffer);
        }

    }
}
