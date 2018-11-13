using System.Collections.Generic;

namespace Server_Tools.Idrac.Models
{
    public class RaidController
    {
        public string FirmwareVersion { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public int SpeedGbps { get; set; }
        public List<string> SupportedControllerProtocols { get; set; }
        public List<string> SupportedDeviceProtocols { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
