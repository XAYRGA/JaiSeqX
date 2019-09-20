


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI;
using JaiSeqX.JAI.Seq;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SdlDotNet.Core;
using Be.IO;



namespace JaiSeqX
{
    public static class JaiSeqX
    {
     
       
        static void Main(string[] args)
        {

         

#if DEBUG
            args = new string[4];
            args[0] = "visu";
            args[1] = "jaiinit.baa";
            args[2] = "0";
            args[3] = "iplrom.bms.bak";
#endif
            var w = File.ReadAllBytes("jaiinit.aaf");
            var b = new JAI.Loaders.JAV1_AAFLoader();
            var wx = new MemoryStream(w);
            var bread = new BeBinaryReader(wx);
            var wat = b.load(ref w);
            for  (int i=0;  i <  wat.Length; i++)
            {
                var data = wat[i];
                Console.WriteLine("{0} {1:X} {2:X} {3}", wat[i].type, wat[i].start, wat[i].size,wat[i].order);
                if (data.type==JAI.Types.JAIInitSectionType.WSYS)
                {
                    var vb = new JAI.Loaders.JAV1_WSYSLoader();
                    bread.BaseStream.Position = data.start;
                    vb.loadWSYS(bread, data.start);
                }
                if (data.type == JAI.Types.JAIInitSectionType.IBNK)
                {
                    var vb = new JAI.Loaders.JAV1_IBankLoader();
                    bread.BaseStream.Position = data.start;
                    var ibnk = vb.loadIBNK(bread, data.start);
                    for (int x=0; x < ibnk.Instruments.Length; x++)
                    {
                        var cinst = ibnk.Instruments[x];
                       
                        if (cinst!=null  && cinst.oscillatorCount > 0)
                        {
                            var cosc = cinst.oscillators[0];
                            if (cosc.ASVector.Length > 1)
                            {
                                cosc.attack();
                                Console.WriteLine("OSCILLATOR BREAK MODE 2");
                                while (true)
                                {
                              
                                    cosc.advance();
                                    var xv = Console.ReadKey();
                                    if (xv.Key==ConsoleKey.C)
                                    {
                                        Console.WriteLine("Oscillator skipped.");
                                        break;
                                    }
                                    if (xv.Key==ConsoleKey.R)
                                    {
                                        cosc.release();
                                        Console.WriteLine("oscillator swap vector: release");
                                    }
                                }
                            }
                        }
                    }
                    
                }
            }

           
            Console.ReadLine();
          

        }
    }
}
