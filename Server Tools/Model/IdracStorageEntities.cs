﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Model
{
    class IdracStorageEntities
    {
        [JsonProperty("@odata.id")]
        public string Id { get; set; }
    }
}
