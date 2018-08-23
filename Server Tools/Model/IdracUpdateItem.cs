using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Model
{
    class IdracUpdateItem
    {

        private Server server;
        private IdracFirmware firmware;
        private string serverName;
        private string firmwareName;
        private string currentVersion;
        private string avaliableVersion;

        public string ServerName { get => serverName; set => serverName = value; }
        public string FirmwareName { get => firmwareName; set => firmwareName = value; }
        public string CurrentVersion { get => currentVersion; set => currentVersion = value; }
        public string AvaliableVersion { get => avaliableVersion; set => avaliableVersion = value; }
        internal Server Server { get => server; set => server = value; }
        internal IdracFirmware Firmware { get => firmware; set => firmware = value; }

        public IdracUpdateItem(Server server, IdracFirmware firmware)
        {
            this.server = server;
            this.firmware = firmware;
            serverName = server.Host;
            firmwareName = firmware.Firmware;
            currentVersion = firmware.CurrentVersion;
            avaliableVersion = firmware.AvaliableVersion;
        }


    }
}
