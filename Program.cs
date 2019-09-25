


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




namespace JaiSeqX
{
    public static class JaiSeqX
    {
        public static AABase AAData;
       
        static void Main(string[] args)
        {

         

#if DEBUG
            args = new string[4];
            args[0] = "visu";
            args[1] = "jaiinit_sms.aaf";
            args[2] = "0";
            args[3] = "test.bms";
#endif
          
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

                if (args[0]=="play")
                {

                    JAIVersion SequencerVersion = JAIVersion.UNKNOWN;
                    if (args.Length < 3)
                    {
                        Console.WriteLine("You need to specify JAIInitFile and JAIVersion");
                        Console.WriteLine("JaiSeqX.exe play JaiInit.aaf 0 boolossus.bms yes");
                        Environment.Exit(-1);
                    }

                    try
                    {
                        SequencerVersion = (JAIVersion)(Convert.ToUInt16(args[2]));
                    }
                    catch
                    {
                        Console.WriteLine("{0} is not a valid JAIVersion", args[2]);
                    }
                    if (SequencerVersion == JAIVersion.UNKNOWN)
                    {
                        Console.WriteLine("{0} is not a valid JAIVersion", args[2]);
                    }

                    if (SequencerVersion == JAIVersion.ONE)
                    {
                        var b = new AAFFile();
                        b.LoadAAFile(args[1], JAIVersion.ONE);
                        AAData = b;
                    }
                    else
                    {
                        var b = new BAAFile();
                        b.LoadBAAFile(args[1], SequencerVersion);
                        AAData = b;
                    }
                    // 

                    if (args.Length < 4)
                    {
                        Console.WriteLine("No BMS file specified.");

                        Environment.Exit(-1);
                    }

                    Player.BMSPlayer.LoadBMS(args[3], ref AAData);
                }

                if (args[0] == "visu")
                {

                    JAIVersion SequencerVersion = JAIVersion.UNKNOWN;
                    if (args.Length < 3)
                    {
                        Console.WriteLine("You need to specify JAIInitFile and JAIVersion");
                        Console.WriteLine("JaiSeqX.exe visu JaiInit.aaf 0 boolossus.bms yes");
                        Environment.Exit(-1);
                    }

                    try
                    {
                        SequencerVersion = (JAIVersion)(Convert.ToUInt16(args[2]));
                    }
                    catch
                    {
                        Console.WriteLine("{0} is not a valid JAIVersion", args[2]);
                    }
                    if (SequencerVersion == JAIVersion.UNKNOWN)
                    {
                        Console.WriteLine("{0} is not a valid JAIVersion", args[2]);
                    }

                    if (SequencerVersion == JAIVersion.ONE)
                    {
                        var b = new AAFFile();
                        b.LoadAAFile(args[1], JAIVersion.ONE);
                        AAData = b;
                    }
                    else
                    {
                        var b = new BAAFile();
                        b.LoadBAAFile(args[1], SequencerVersion);
                        AAData = b;
                    }
                    // 

                    if (args.Length < 4)
                    {
                        Console.WriteLine("No BMS file specified.");
                        Environment.Exit(-1);
                    }
                    Player.BMSVisualizer.Init();
                    
                    Player.BMSPlayer.LoadBMS(args[3], ref AAData);

                }
            } else
            {
                Console.WriteLine("JaiSeqX [command] <args>");
                Console.WriteLine();
                Console.WriteLine("JaiSeqX mkjasm <file> -  Creates a .JASM (Jai Assembly) file from file.bms");
                Console.WriteLine("\tJaiSeqX mkjasm file.bms\t");
                Console.WriteLine();
                Console.WriteLine("JaiSeqX play <aafFile> <JaiVersion> <BMS file> - Plays a BMS file with the specified JaiInit.aaf and version");
                Console.WriteLine("\tJaiSeqX play JaiInit.aaf 0 file.bms");
                Console.WriteLine("\t For JaiVersion info, see JaiVersion.cs or JV.txt! ");
                Console.WriteLine();
                Console.WriteLine("JaiSeqX visu <aafFile> <JaiVersion> <BMS file> - Plays a BMS file with visualizer, same as command above but with visualizer.");
                Console.WriteLine("\tJaiSeqX play JaiInit.aaf 0 file.bms");
                Console.WriteLine("\t For JaiVersion info, see JaiVersion.cs or JV.txt! ");
       
            }

        }
    }
}
