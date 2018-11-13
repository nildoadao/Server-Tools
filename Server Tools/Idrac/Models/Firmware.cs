using Newtonsoft.Json;

namespace Server_Tools.Idrac.Models
{
    public class Firmware
    {
        [JsonProperty("@odata.id")]
        public string OdataId { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public bool Updateable { get; set; }
        public string Version { get; set; }
    }
}
