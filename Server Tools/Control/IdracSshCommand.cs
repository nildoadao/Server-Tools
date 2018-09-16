using Renci.SshNet;
using System;
using Server_Tools.Model;

namespace Server_Tools.Control
{
    class IdracSshCommand : IDisposable
    {
        private SshClient client;
        private Server server;

        public IdracSshCommand(Server server)
        {
            this.server = server;
            client = new SshClient(server.Host, server.User, server.Password);
            client.Connect();
        }

        public void Dispose()
        {
            if(client != null)
            {
                client.Dispose();
            }
        }

        public SshCommand RunCommand(string command)
        {
            SshCommand commandResult = client.RunCommand(command);
            if(commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }
    }
}