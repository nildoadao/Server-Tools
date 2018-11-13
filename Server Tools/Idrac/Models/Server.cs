
namespace Server_Tools.Idrac.Models
{
    public class Server
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
