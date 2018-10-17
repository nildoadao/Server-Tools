using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Models
{
    class Enclousure
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<OdataObject> Drives { get; set; }
        public List<RaidController> StorageControllers { get; set; }
        public OdataObject Volumes { get; set; }
    }
}
