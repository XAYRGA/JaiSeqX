// See https://aka.ms/new-console-template for more information

using Be.IO;
using jaudio;

public static class JAISeqX
{
    public static void Main(string[] args)
    {
        Console.WriteLine("JAISEQX");
        {
            var fl = File.OpenRead("66.bnk");
            var br = new BeBinaryReader(fl);
            var bank = JInstrumentBankv2.CreateFromStream(br);
            Console.WriteLine("PARSE OK!");
            foreach (KeyValuePair<int, JEnvelope> kvp in bank.Envelopes)
            {
                Console.WriteLine($"{kvp.Key:X}");
            }
        }

        {
            var fl = File.OpenRead("jaiinit.aaf");
            var br = new BeBinaryReader(fl);
            var bank = AudioArchive.CreateFromStream(br);
            Console.WriteLine("PARSE OK!");
            foreach (AudioArchiveSectionInfo kvp in bank.Sections)
            {
                Console.WriteLine($"{kvp.type}");
            }

        }
       

      
    }
}





