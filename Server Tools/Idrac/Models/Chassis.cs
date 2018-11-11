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
            public List<OdataObject> ComputerSystems { get; }
            public List<OdataObject> ContainedBy { get; }
            public List<OdataObject> Contains {get; }
            public List<OdataObject> CooledBy{ get; }
            public List<OdataObject> Drives {get; }
            public List<OdataObject> ManagedBy { get; }
            public List<OdataObject> ManagersInChassis { get; }
            public List<OdataObject> PCIeDevices { get; }
            public List<OdataObject> PoweredBy { get; }
            public List<OdataObject> ResourceBlocks { get; }
            public List<OdataObject> Storage { get; }
            public List<OdataObject> Switches { get; }
        }

        public ChassisLink Links { get ;}
        public string AssetTag { get; }
        public string ChassisType { get; }
        public OdataObject LogServices { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public OdataObject NetworkAdapters { get; }
        public string PartNumber { get; }
        public OdataObject Power { get; }
        public string SKU { get; }
        public string SerialNumber { get; }
        public OdataObject Thermal { get; }
        public string UUID { get; }    
    }
}
