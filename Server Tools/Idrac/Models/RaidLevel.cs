
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
