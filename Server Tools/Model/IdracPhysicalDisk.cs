using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Model
{
    class IdracPhysicalDisk
    {
        string name;
        string size;
        string usedRaidSpace;
        string freeRaidSpace;
        bool isAssigned;

        public string Name { get => name; set => name = value; }
        public string Size { get => size; set => size = value; }
        public string UsedRaidSpace { get => usedRaidSpace; set => usedRaidSpace = value; }
        public string FreeRaidSpace { get => freeRaidSpace; set => freeRaidSpace = value; }
        public bool IsAssigned { get => isAssigned; set => isAssigned = value; }

        public IdracPhysicalDisk(string name, string size)
        {
            this.name = name;
            this.size = size;
            usedRaidSpace = "";
            freeRaidSpace = "";
            isAssigned = true;
        }

    }
}
