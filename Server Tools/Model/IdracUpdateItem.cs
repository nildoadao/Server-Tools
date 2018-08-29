
namespace Server_Tools.Model
{
    class IdracUpdateItem
    {

        private Server server;
        private IdracFirmware firmware;
        private bool update;

        public bool Update { get => update; set => update = value; }
        public string ServerName { get => server.Host; }
        public string FirmwareName { get => firmware.Firmware;  }
        public string CurrentVersion { get => firmware.CurrentVersion;  }
        public string AvaliableVersion { get => firmware.AvaliableVersion; }
        public string FirmwarePath { get => firmware.FirmwarePath; }      

        internal Server Server { get => server; set => server = value; }
        internal IdracFirmware Firmware { get => firmware; set => firmware = value; }

        public IdracUpdateItem(Server server, IdracFirmware firmware)
        {
            this.server = server;
            this.firmware = firmware;
            update = false;
        }


    }
}
