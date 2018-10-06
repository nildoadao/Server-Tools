using Renci.SshNet;
using Server_Tools.Model;

namespace Server_Tools.Idrac
{
    class IdracSshCommand
    {
        private Server server;

        public IdracSshCommand(Server server)
        {
            this.server = server;
        }

        public string RunCommand(string command)
        {
            using (SshClient client = new SshClient(server.Host, server.User, server.Password))
            {
                client.Connect();
                SshCommand commandResult = client.RunCommand(command);

                if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
                    throw new RacadmException(commandResult.Result);

                return commandResult.Result;
            }
        }
    }
}