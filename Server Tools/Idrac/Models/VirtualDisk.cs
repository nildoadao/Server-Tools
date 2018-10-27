using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Models
{
    public class VirtualDisk
    {
        [JsonProperty("@odata.id")]
        public OdataObject OdataId { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public long BlockSizeBytes { get; set; }
        public long CapacityBytes { get; set; }
        public long OptimumIOSizeBytes { get; set; }
        public string VolumeType { get; set; }
        public VolumeLink Links { get; set; }

        public class VolumeLink
        {
            public List<OdataObject> Drives { get; set; }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
