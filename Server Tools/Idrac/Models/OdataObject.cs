using Newtonsoft.Json;

namespace Server_Tools.Idrac.Models
{
    public class OdataObject
    {
        [JsonProperty("@odata.id")]
        public string Id { get; set; }
    }
}
