using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO; 

namespace JaiSeqX.JAI
{
    public static class Helpers
    {
        public static uint ReadUInt24BE(BinaryReader reader)
        {
            try
            {
                var b1 = reader.ReadByte();
                var b2 = reader.ReadByte();
                var b3 = reader.ReadByte();
                return
                    (((uint)b1) << 16) | // fuuck.
                    (((uint)b2) << 8) | // FfFFFuuuuuuck.
                    ((uint)b3); // FFFFUUUUUUUUUUUCK.
            }
            catch
            {
                return 0u;
            }
        }

        public static int ReadVLQ(BinaryReader reader)
        {
            int fade = (int)reader.ReadByte();
            while ((fade & 0x80) > 0)
            {
                fade = ((fade & 0x7F) << 7);
                fade += reader.ReadByte();


            }
            return fade;
        }

        public static string readArchiveName(BinaryReader aafRead)
        {
            var ofs = aafRead.BaseStream.Position;
            byte nextbyte;
            byte[] name = new byte[0x70];

            int count = 0;
            while ((nextbyte = aafRead.ReadByte()) != 0xFF & nextbyte != 0x00)
            {
                name[count] = nextbyte;
                count++;
            }
            aafRead.BaseStream.Seek(ofs + 0x70, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(name, 0, count);
        }









        /* I'll start by being honest.
         * I have absolutely no clue how ADPCM works, nor do i have any interest in this..... garbage.
         * This is basically a miniaturized version of WWDumpSound, used code from Arookas and Jasper (magcius)
         * I just modified it to work with BMSXPX / JaiSeqX
         */

        static ushort[] afccoef = new ushort[16]
      {
            0,
            0x0800,
            0,
            0x0400,
            0x1000,
            0x0e00,
            0x0c00,
            0x1200,
            0x1068,
            0x12c0,
            0x1400,
            0x0800,
            0x0400,
            0xfc00,
            0xfc00,
            0xf800,
          //? Array error
      };

       static  ushort[] afccoef2 = new ushort[16]
        {
            0,
            0,
            0x0800,
            0x0400,
            0xf800,
            0xfa00,
            0xfc00,
            0xf600,
            0xf738,
            0xf704,
            0xf400,
            0xf800,
            0xfc00,
            0x0400,
            0,
            0,
        };



        public static string AFCtoPCM16(byte[] adpcm, double srate ,int vsize, ushort format,string pth_out)
        {

            var fobj_reader = new BeBinaryReader(new MemoryStream(adpcm));
            var fobj_writer_stream = File.Open(pth_out, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var fobj_writer = new BinaryWriter(fobj_writer_stream);
            short[] data_out;
            var total = 0;

            /* Below is SHAMELESSLY ripped from WWDumpSND */
            unchecked
            {

                /******* DECODE AFC TO PCM *********/

                int hi0 = 0;
                int hi1 = 0;
     
                int framesz = 9;
                int osz = (int)vsize / framesz * 16 * 2;
                int oszt = osz + 8;
                int size_rem;
                short[] wavout;
                byte[] wavin;

                /////****** WAV BUFFER ******/////

                byte[] wavhead = new byte[44] {
                        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,  0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
                        0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,  0x00, 0x00, 0x00, 0x00
                    };

                for (int i = 0; i < wavhead.Length; i++)
                {
                    fobj_writer.Write(wavhead[i]);

                }

                fobj_writer.BaseStream.Position = 4;
                fobj_writer.Write(oszt);
                fobj_writer.BaseStream.Position = 24;
                fobj_writer.Write((int)srate);
                fobj_writer.Write((int)srate);
                fobj_writer.BaseStream.Position = 40;
                fobj_writer.Write((int)osz);
                fobj_reader.BaseStream.Position = 0;

                byte cbyte = 0;
                sbyte[] nibbles;

                for (size_rem = (int)vsize; size_rem >= framesz; size_rem -= framesz)
                {
                    wavin = fobj_reader.ReadBytes(framesz);
                    wavout = new short[16];
                    var wavreader = new BeBinaryReader(new MemoryStream(wavin));
                    /********* AFC Decoder buffer **********/
                    cbyte = wavreader.ReadByte(); // READ BYTE 0, ADVANCE TO 1
                    int scale = (1 << (cbyte >> 4));
                    //Console.WriteLine("Delta {0} - {1} FSZ {2}", scale, (short)((cbyte) >> 4),framesz);
                    short index = (short)(cbyte & 0xF);
                    //Console.WriteLine("Index {0}", index);
                    nibbles = new sbyte[16];
                    if (format == 0)
                    {
                        for (int i = 0; i < 16; i += 2)
                        {
                            cbyte = wavreader.ReadByte(); // src ++ 
                            var bse = (sbyte)(cbyte);
                            nibbles[i + 0] = (sbyte)(bse >> 4);
                            nibbles[i + 1] = (sbyte)(cbyte & 15);
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            if (nibbles[i] >= 8)
                            {
                                nibbles[i] = (sbyte)(nibbles[i] - 16);
                            }
                        }
                    }
                    else
                    {
                        /*
                        for (int i = 0; i < 16; i += 4)
                        {
                            nibbles[i + 0] = (short)((cbyte >> 6) & 0x03);
                            nibbles[i + 1] = (short)((cbyte >> 4) & 0x03);
                            nibbles[i + 2] = (short)((cbyte >> 2) & 0x03);
                            nibbles[i + 3] = (short)((cbyte >> 0) & 0x03);
                            
                            cbyte = wavreader.ReadByte(); // src ++ 
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            if (nibbles[i] >= 2)
                            {
                                nibbles[i] = (short)(nibbles[i] - 4);
                                nibbles[i] = (short)(nibbles[i] << 13);
                            }
                        }
                    }


                        */

                    }


                    for (int i = 0; i < 16; i++)
                    {
                        //Console.WriteLine(nibbles[i]);

                        var superscale = ((scale * nibbles[i]) << 11);
                        var coef1_addi = (int)hi0 * (short)afccoef[index];
                        var coef2_addi = (int)hi1 * (short)afccoef2[index];
                        var final0 = superscale + coef1_addi + coef2_addi;
                        var final1 = final0 >> 11;
                        int sample = final1;
                        //Console.ReadLine();
                        // CLAMP 16 BIT PCM
                        //Console.WriteLine("XATA scc {0} c1a {1} c2a {2} f0 {3} f1 {4}", superscale,coef1_addi,coef2_addi,final0,final1);
                        //Console.WriteLine("Data scl {0} nibi {1} hi0 {2} hi1 {3} c1 {4:X6} c2 {5:X6}", scale, nibbles[i], hi0,hi1,afccoef[index],afccoef2[index]);
                        //Console.WriteLine("Sample {0}", final1);
                        //Console.ReadLine();

                        if (sample > 32767)
                        {
                            sample = 32767;
                        }
                        if (sample < -32768)
                        {
                            sample = -32768;
                        }
                        wavout[i] = (short)(sample);
                        hi1 = hi0;
                        hi0 = sample;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        fobj_writer.Write(wavout[i]);
                    }
                }

                // Console.WriteLine("osz {0} {1}", osz, fobj_writer.BaseStream.Length);
                fobj_writer.Flush();
                fobj_writer.Close();
            }

            return pth_out;
        }



        public static void printJaiSeqStack(JAI.Seq.Subroutine Seq)
        {
            var opstack = Seq.OpcodeHistory;
            var postack = Seq.OpcodeAddressStack;
            opstack = new Queue<byte>(opstack.Reverse());
            postack = new Queue<int>(postack.Reverse());
            int finalCall = 0; 
            int finalCallAddr = 0; 

            try
            {
                finalCall = opstack.Dequeue();
                finalCallAddr = postack.Dequeue();
                
            } catch
            {
                Console.WriteLine("JaiSeqXHelpers: Couldn't print JaiSeq stack. There's probably nothing in the stack.");
                return;
            }

            var depth = 0;
            Console.WriteLine("===== printJaiSeqStack");
            Console.WriteLine("(depth) addr: opcode");
            var b = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("({2:X}) 0x{1:X}: 0x{0:X}", finalCall, finalCallAddr, depth);
            Console.ForegroundColor = b; 
            while (true)
            {
                depth++;
                if (opstack.Count == 0) { break; }
                finalCall = opstack.Dequeue();
                finalCallAddr = postack.Dequeue();
                Console.WriteLine("\t({2:X}) 0x{1:X}: 0x{0:X}", finalCall, finalCallAddr,depth);
            }
        }

    }
}
