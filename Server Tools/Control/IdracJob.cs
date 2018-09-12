using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Control
{
    class IdracJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Messages { get; set; }
        public string TaskState { get; set; }
    }
}
