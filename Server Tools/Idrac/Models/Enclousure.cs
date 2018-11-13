using Newtonsoft.Json;
using System.Collections.Generic;


namespace Server_Tools.Idrac.Models
{
    public class Enclousure
    {
        [JsonProperty("@odata.id")]
        public string OdataId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<OdataObject> Drives { get; set; }
        public List<RaidController> StorageControllers { get; set; }
        public OdataObject Volumes { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
