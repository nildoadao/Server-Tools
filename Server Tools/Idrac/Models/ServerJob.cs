using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Models
{
    class ServerJob
    {
        public Server Server { get; set; }
        public IdracJob Job { get; set; }
    }
}
