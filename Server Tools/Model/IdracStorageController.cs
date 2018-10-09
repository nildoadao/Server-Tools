using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Model
{
    class IdracStorageController
    {
        public string FirmwareVersion { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public int SpeedGbps { get; set; }
        public List<string> SupportedControllerProtocols { get; set; }
        public List<string> SupportedDeviceProtocols { get; set; }
    }
}
