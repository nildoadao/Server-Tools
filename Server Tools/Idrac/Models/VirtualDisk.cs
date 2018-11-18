using Newtonsoft.Json;
using System.Collections.Generic;

namespace Server_Tools.Idrac.Models
{
    public class VirtualDisk
    {
        [JsonProperty("@odata.id")]
        public string OdataId { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public long BlockSizeBytes { get; set; }
        public long CapacityBytes { get; set; }
        public long OptimumIOSizeBytes { get; set; }
        public string VolumeType { get; set; }
        public string FormatedType { get { return ParseRaid(VolumeType); } }
        public VolumeLink Links { get; set; }

        public class VolumeLink { public List<OdataObject> Drives { get; set; } }

        public override string ToString()
        {
            return Description;
        }

        // Converte a descrição do volume para o tipo de Raid
        // ex: Mirrored = Raid 1

        private string ParseRaid(string value)
        {
            Dictionary<string, string> levels = new Dictionary<string, string>()
            {
                {"NonRedundant", "Raid 0" },
                {"Mirrored", "Raid 1" },
                {"StripedWithParity", "Raid 5" },
                {"SpannedMirrors", "Raid 10" },
                {"SpannedStripesWithParity", "Raid 50" }
            };
            return levels[value];
        }
    }
}
