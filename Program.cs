


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
            var wat = b.load(ref w);
            for  (int i=0;  i <  wat.Length; i++)
            {
                Console.WriteLine("{0} {1:X} {2:X} {3}", wat[i].type, wat[i].start, wat[i].size,wat[i].order);
            }

           
            Console.ReadLine();
          

        }
    }
}
