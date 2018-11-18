using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Server_Tools.Idrac.Models
{
    public class PhysicalDisk
    {
        [JsonProperty("@odata.id")]
        public string OdataId { get; set; }
        public string Id { get; set; }
        public long CapacityBytes { get; set; }
        public string FormatedCapacity { get { return ParseBytes(CapacityBytes); } }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public string BlockSizeBytes { get; set;}
        public int CapableSpeedGbs { get; set; }
        public int NegotiatedSpeedGbs { get; set; }
        public bool FailurePredicted { get; set; }
        public string HotspareType { get; set; }
        public string Protocol { get; set; }
        public string SerialNumber { get; set; }
        public int RotationSpeedRPM { get; set; }
        public DiskLink Links { get; set; }
        
        public class DiskLink { public List<OdataObject> Volumes { get; set; } }

        public override string ToString()
        {
            return Name;
        }

        // Converte o valor em Bytes para uma notação abreviada
        // ex: 44.040.192 Bytes para 42 MB

        private string ParseBytes(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", "O valor para conversão não pode ser negativo");

            Dictionary<int, string> sizes = new Dictionary<int, string>()
            {
                {0, "B"},
                {1, "KB"},
                {2, "MB"},
                {3, "GB"},
                {4, "TB"},
                {5, "PB"},
                {6, "EB"},
            };

            long x = value;
            int count = 0;
            while (x > 1024)
            {
                x = x / 1024;
                count++;
            }
            return string.Format("{0} {1}", x.ToString(), sizes[count]);
        }
    }
}
