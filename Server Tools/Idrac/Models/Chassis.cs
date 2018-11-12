using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Models
{
    public class Chassis
    {
        public class ChassisLink
        {
            public List<OdataObject> ComputerSystems { get; set; }
            public List<OdataObject> ContainedBy { get; set; }
            public List<OdataObject> Contains {get; set; }
            public List<OdataObject> CooledBy{ get; set; }
            public List<OdataObject> Drives {get; set; }
            public List<OdataObject> ManagedBy { get; set; }
            public List<OdataObject> ManagersInChassis { get; set; }
            public List<OdataObject> PCIeDevices { get; set; }
            public List<OdataObject> PoweredBy { get; set; }
            public List<OdataObject> ResourceBlocks { get; set; }
            public List<OdataObject> Storage { get; set; }
            public List<OdataObject> Switches { get; set; }
        }

        public ChassisLink Links { get; set; }
        public string AssetTag { get; set; }
        public string ChassisType { get; set; }
        public OdataObject LogServices { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public OdataObject NetworkAdapters { get; set; }
        public string PartNumber { get; set; }
        public OdataObject Power { get; set; }
        public string SKU { get; set; }
        public string SerialNumber { get; set; }
        public OdataObject Thermal { get; set; }
        public string UUID { get; set; }    
    }
}
