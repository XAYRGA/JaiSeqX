using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Types;

namespace JaiSeqX.JAI
{
    public class JASystem
    {
        public JIBank[] Banks;
        public JWaveSystem[] WaveBanks;
        public JAIVersion version;
        
    }

    public enum JAIVersion
    {
        // Games that use JAIV1 
        // Luigis Mansion
        // Super Mario Sunshine
        // Pikmin
        // Pikmin 2
        // Legend of Zelda: Windwaker 
        ZERO = 0,
        // Games that use JAIV2 
        // Mario Kart Double Dash
        ONE = 1,
        // Games that use JAIV3 
        // Super Mario Galaxy 1 & 2 
        // Legend of Zelda: Twilight Princess
        TWO = 2
    }

}
