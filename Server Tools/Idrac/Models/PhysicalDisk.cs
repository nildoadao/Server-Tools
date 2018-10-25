
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Server_Tools.Idrac.Models
{
    public class PhysicalDisk
    {
        [JsonProperty("@odata.id")]
        public string OdataId { get; set; }
        public string Id { get; set; }
        public long CapacityBytes { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public string BlockSizeBytes { get; set;}
        public int CapableSpeedGbs { get; set; }
        public int NegotiatedSpeedGbs { get; set; }
        public bool FailurePredicted { get; set; }
        public string HotspareType { get; set; }
        public string Protocol { get; set; }
        public string SerialNumber { get; set; }
        public int RotationSpeedRPM { get; set; }
        public DiskLink Links { get; set; }
        
        public class DiskLink
        {
            public List<OdataObject> Volumes { get; set; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
