using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Model
{
    class Server
    {
        private string host;
        private string user;
        private string password;

        public string Host { get => host; set => host = value; }
        public string User { get => user; set => user = value; }
        public string Password { get => password; set => password = value; }

        public Server(string host, string user, string password)
        {
            this.host = host;
            this.user = user;
            this.password = password;
        }

        public override string ToString()
        {
            return Host;
        }
    }
}
