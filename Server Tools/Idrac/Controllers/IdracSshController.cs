﻿using Renci.SshNet;
using Server_Tools.Idrac.Models;

namespace Server_Tools.Idrac.Controllers
{
    class IdracSshController
    {
        private Server server;

        /// <summary>
        /// Inicializa uma nova instancia de IdracSshController
        /// </summary>
        /// <param name="server">Objeto contendo os dados do servidor</param>
        public IdracSshController(Server server)
        {
            this.server = server;
        }

        /// <summary>
        /// Executa um comando via SSH em um servidor remoto
        /// </summary>
        /// <param name="command">String contendo o comando a ser executado</param>
        /// <returns>String contendo o resultado do comando</returns>
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