using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX
{
    public enum JAIVersion
    {
        // Games that use JAIV1 
        // Luigis Mansion
        // Super Mario Sunshine
        // Pikmin
        // Pikmin 2
        ONE = 0x00, 
        
        // Games that use JAIV2 
        // Mario Kart Double Dash
        TWO = 0x01,

        // Games that use JAIV3 
        // Super Mario Galaxy 1 & 2 
        // Legend of Zelda: Windwaker 
        // Legend of Zelda: Twilight Princess
        THREE = 0x02, 

        UNKNOWN = 0xFF,
    }
}
