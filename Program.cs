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

namespace JaiSeqX
{
    public static class JaiSeqX
    {
        public static AABase AAData;
       
        static void Main(string[] args)
        {
            //Console.ReadLine();
            if (args.Length > 0)
            {
                if (args[0] == "mkjasm")
                {
                    if (args.Length > 2)
                    {
                        var wtf = new JASM.JASMConverter(args[1], args[2]);
                        return; 
                    }

                }

            }




            

 #if DEBUG
             args = new string[2];
             args[0] = "JaiInit.aaf";
             args[1] = "0"; 
 #endif
             JAIVersion SequencerVersion = JAIVersion.UNKNOWN; 
            if (args.Length < 2)
             {
                 Console.WriteLine("You need to specify JAIInitFile and JAIVersion");
                 Environment.Exit(-1);
             }

            try
             {
                 SequencerVersion = (JAIVersion)(Convert.ToUInt16(args[1]));
             } catch
             {
                 Console.WriteLine("{0} is not a valid JAIVersion", args[1]);
             }
             if (SequencerVersion==JAIVersion.UNKNOWN)
             {
                 Console.WriteLine("{0} is not a valid JAIVersion", args[1]);
             }

             if (SequencerVersion==JAIVersion.ONE)
             {
                 var b = new AAFFile();
                 b.LoadAAFile(args[0],JAIVersion.ONE);
                 AAData = b;
             } else
             {
                 var b = new BAAFile();
                 b.LoadBAAFile(args[0], SequencerVersion);
                 AAData = b;
             }
            // 





            Player.BMSVisualizer.Init();

            Console.ReadLine();

            Player.BMSPlayer.LoadBMS("BlueBattle.bms", ref AAData);





        }
    }
}
