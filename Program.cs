using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI;

namespace JaiSeqX
{
    class JaiSeqX
    {
    
        static void Main(string[] args)
        {
            var b = new AAFFile();
            b.LoadAAFile("JaiInit.aaf", JAIVersion.ONE);

            Console.ReadLine();

        }
    }
}
