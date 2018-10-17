﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Models
{
    class VirtualDisk
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public long BlockSizeBytes { get; set; }
        public long CapacityBytes { get; set; }
        public long OptimumIOSizeBytes { get; set; }
        public VolumeLink Links { get; set; }

        internal class VolumeLink
        {
            public List<OdataObject> Drives { get; set; }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
