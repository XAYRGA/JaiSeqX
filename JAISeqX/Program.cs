// See https://aka.ms/new-console-template for more information

using Be.IO;
using jaudio;

public static class JAISeqX
{
    public static void Main(string[] args)
    {
        /*
        Console.WriteLine("JAISEQX");
        {
            var fl = File.OpenRead("4.bnk");
            var br = new BeBinaryReader(fl);
            var bank = JInstrumentBankv2.CreateFromStream(br);
            Console.WriteLine("PARSE OK!");
            foreach (KeyValuePair<int, JEnvelope> kvp in bank.Envelopes)
            {
                Console.WriteLine($"{kvp.Key:X}");
            }
        }*/


        Console.WriteLine("===== Test parse AAF ===== ");
        {
            var fl = File.OpenRead("jaiinit.aaf");
            var br = new BeBinaryReader(fl);
            var bank = AudioArchive.CreateFromStream(br);
            Console.WriteLine("\tAAF PARSE OK!");
            foreach (AudioArchiveSectionInfo kvp in bank.Sections)

                Console.WriteLine($"\t{kvp.type}");
          
        }

        Console.WriteLine("===== Test parse BAA + Load banks ===== ");
        {
            var fl = File.OpenRead("z2sound.baa");
            var br = new BeBinaryReader(fl);
            var bank = BinaryAudioArchive.CreateFromStream(br);
            Console.WriteLine("\tBAA PARSE OK!");
            var i = 0;
            foreach (AudioArchiveSectionInfo kvp in bank.Sections)
            {
                Console.WriteLine($"\t{kvp.type} {i}");
                if (kvp.type == AAFChunkType.IBNK)
                    JInstrumentBankv2.CreateFromStream(kvp.reader);
                else if (kvp.type == AAFChunkType.WSYS)
                    WaveSystem.CreateFromStream(kvp.reader);
                i++;
            }

        }
        


    }
}





