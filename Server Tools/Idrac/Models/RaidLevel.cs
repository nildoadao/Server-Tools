using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Models
{
    enum RaidLevel
    {
        NonRedundant = 0,
        Mirrored = 1,
        StripedWithParity = 5,
        SpannedMirrors = 10,
        SpannedStripesWithParity = 50
    }
}
