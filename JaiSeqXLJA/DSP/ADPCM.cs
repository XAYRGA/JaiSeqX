using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JaiSeqXLJA.DSP
{
    public static class ADPCM
    {
        public enum ADPCMFormat
        {
            FOUR_BIT = 0,
            TWO_BIT = 3 , 
            TWO_BIT_EXT = 5,
        }
        private static ushort[] COEF0 = new ushort[16]
        {
            0,0x0800, 0,0x0400,
            0x1000,0x0e00,0x0c00,0x1200,
            0x1068,0x12c0,0x1400,0x0800,
            0x0400,0xfc00,0xfc00,0xf800
        };

        private static ushort[] COEF1 = new ushort[16]
         {
            0,0,0x0800,0x0400,0xf800,
            0xfa00,0xfc00,0xf600,0xf738,
            0xf704,0xf400,0xf800,0xfc00,
            0x0400,0,0,
         };


        private static byte[] adpcm4topcm16(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data)); // sprawl array into stream
            int smpIdx = 0; // current sample index (will be * 2)
            int frameSize = 9; // probably should be a const
            byte[] pcmData = new byte[(data.Length / frameSize) * 16 * 2]; //  buffer size / frame length (9) (HEADER, xPCM xPCM xPCM .....) * 16 (16 samples per frame) * 2 bytes for short
            int hist0 = 0; // historical transform distance
            int hist1 = 0;  // historical transform distance


            for (int bufferSizeRemaining = data.Length; bufferSizeRemaining >= frameSize; bufferSizeRemaining -= frameSize)
            {
                var sample = new short[16];
                var frame = reader.ReadByte(); // read frame header
                var frameS = (sbyte)0; // reset signed frame value
                var scale = (1 << (frame >> 4)); // non-coef scale / gain (Exponential)
                short coefIndex = (short)(frame & 0xF); // coefficient index  (Cumulative)\

                for (int i = 0; i < 16; i += 2)
                {
                    frame = reader.ReadByte(); // pos++, read signed frame 
                    frameS = (sbyte)frame;  // sign frame (last bit) NECESSARY BECAUSE C# HAS STICKY SIGN.
                    sample[i] = (sbyte)(frameS >> 4); // extract upper nybble, preserve sign
                    sample[i + 1] = (sbyte)(frame & 15); // extract lower nybble, preserve sign (last bit) 
                }
                for (int i = 0; i < 16; i++)
                {
                    if (sample[i] >= 8) { sample[i] = (sbyte)(sample[i] - 16); } // clamp & wrap unscaled differential transform 
                }

                // *** UNTRASFORM ***
                // (FREQUENCY TRANSFORM) 
                for (int i = 0; i < 16; i++)
                {
                    var gained = (scale * sample[i]) << 11; // extend (for sign scale)
                    var coef0S = (int)hist0 * (short)COEF0[coefIndex]; // grab historical transofrm scale (max dist)
                    var coef1S = (int)hist1 * (short)COEF1[coefIndex]; // grab historical transform scale( max dist, second index)
                   
                    var pcmSample = (gained + coef0S + coef1S) >> 11;  // return scaled value (sign sticks)
                    //Console.WriteLine(pcmSample);
                    if (pcmSample > 32767) { pcmSample = 32767; } // clamp 32767+
                    if (pcmSample < -32768) { pcmSample = -32768; } // clamp 32768-
                                                                    // LITTLE ENDIAN, bigger value first
                    pcmData[smpIdx] = (byte)(pcmSample & 0xFF); // upper byte of short, these are potentially reversed.
                                                                // smaller value next
                    pcmData[smpIdx + 1] = (byte)(pcmSample >> 8); // lower byte of short 
                    hist1 = hist0;
                    hist0 = pcmSample;
                    smpIdx += 2;
                }
                
            }

            return pcmData;
        }



        private static byte[] adpcm2topcm16(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data)); // sprawl array into stream
            int smpIdx = 0; // current sample index (will be * 2)
            int frameSize = 9; // probably should be a const
            byte[] pcmData = new byte[(data.Length / frameSize) * 16 * 2]; //  buffer size / frame length (9) (HEADER, xPCM xPCM xPCM .....) * 16 (16 samples per frame) * 2 bytes for short
            int hist0 = 0; // historical transform distance
            int hist1 = 0;  // historical transform distance


            for (int bufferSizeRemaining = data.Length; bufferSizeRemaining >= frameSize; bufferSizeRemaining -= frameSize)
            {
                var sample = new short[16];
                var frame = reader.ReadByte(); // read frame header
                var frameS = (sbyte)0; // reset signed frame value
                var scale = (1 << (frame >> 4)); // non-coef scale / gain (Exponential)
                short coefIndex = (short)(frame & 0xF); // coefficient index  (Cumulative)\
                for (int i = 0; i < 16; i += 4)
                {
                    sample[i + 0] = (short)((sbyte)(frame >> 6) & 0x03);
                    sample[i + 1] = (short)((sbyte)(frame >> 4) & 0x03);
                    sample[i + 2] = (short)((sbyte)(frame >> 2) & 0x03);
                    sample[i + 3] = (short)((sbyte)(frame >> 0) & 0x03);
                    frame = reader.ReadByte(); // src ++ 
                }

                for (int i = 0; i < 16; i++)
                {
                    if (sample[i] >= 2)
                    {
                        sample[i] = (short)(sample[i] - 4);
                        sample[i] = (short)(sample[i] << 13);
                    }
                }
            // *** UNTRASFORM ***
            // (FREQUENCY TRANSFORM) 
            for (int i = 0; i < 16; i++)
                {
                    var gained = (scale * sample[i]) << 11; // extend (for sign scale)
                    var coef0S = hist0 * (short)COEF0[coefIndex]; // grab historical transofrm scale (max dist)
                    var coef1S = hist1 * (short)COEF1[coefIndex]; // grab historical transform scale( max dist, second index)
                    var pcmSample = (gained + coef0S + coef1S) >> 11;  // return scaled value (sign sticks)
                    if (pcmSample > 32767) { pcmSample = 32767; } // clamp 32767+
                    if (pcmSample < -32768) { pcmSample = -32768; } // clamp 32768-
                                                                    // LITTLE ENDIAN, bigger value first
                    pcmData[smpIdx] = (byte)(pcmSample & 0xFF); // upper byte of short, these are potentially reversed.
                                                                // smaller value next
                    pcmData[smpIdx + 1] = (byte)(pcmSample >> 8); // lower byte of short 
                    hist1 = hist0;
                    hist0 = pcmSample;
                    smpIdx += 2;
                }

            }

            return pcmData;
        }


        public static byte[] ADPCMToPCM16(byte[] data, ADPCMFormat format)
        {
            switch (format)
            {
                case ADPCMFormat.FOUR_BIT:                        
                    return adpcm4topcm16(data);                        
                case ADPCMFormat.TWO_BIT_EXT:
                case ADPCMFormat.TWO_BIT:
                    return adpcm2topcm16(data);
                default:
                    return new byte[0];
            }     
        }
    }
}
