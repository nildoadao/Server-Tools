using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools
{
    class IdracFirmware
    {

        bool update;
        string firmwareName;
        string currentVersion;
        string avaliableVersion;
        string firmwarePath;

        public bool Update { get => update; set => update = value; }
        public string Firmware { get => firmwareName; set => firmwareName = value; }
        public string CurrentVersion { get => currentVersion; set => currentVersion = value; }
        public string AvaliableVersion { get => avaliableVersion; set => avaliableVersion = value; }
        public string FirmwarePath { get => firmwarePath; set => firmwarePath = value; }

        public IdracFirmware(bool update, string firmwareName)
        {
            this.firmwareName = firmwareName;
            this.update = update;
        }
    }
}
