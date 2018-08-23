
namespace Server_Tools.Model
{
    class Repository
    {
        string address;
        string user;
        string password;
        RepositoryType type;
        string proxyAddres;
        string proxyUser;
        string proxyPassword;
        string proxyPort;
        string proxyType;
        bool isPasswordProtected;

        public string Address { get => address; set => address = value; }

        public string User
        {
            get => user;
            set
            {
                user = value;
                IsPasswordProtected = true;
            }
        }
        public string Password { get => password; set => password = value; }
        internal RepositoryType Type { get => type; set => type = value; }
        public string ProxyAddres { get => proxyAddres; set => proxyAddres = value; }
        public string ProxyUser { get => proxyUser; set => proxyUser = value; }
        public string ProxyPassword { get => proxyPassword; set => proxyPassword = value; }
        public string ProxyPort { get => proxyPort; set => proxyPort = value; }
        public string ProxyType { get => proxyType; set => proxyType = value; }
        public bool IsPasswordProtected { get => isPasswordProtected; private set => isPasswordProtected = value; }

        public Repository(string repositoryAddress, RepositoryType type)
        {
            address = repositoryAddress;
            this.type = type;
            user = "";
            password = "";
            proxyAddres = "";
            proxyUser = "";
            proxyPassword = "";
            proxyPort = "";
            proxyType = "";
            isPasswordProtected = false;
        }
    }
}
