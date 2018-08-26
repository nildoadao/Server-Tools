using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server_Tools.Model;

namespace Server_Tools.Control
{
    class IdracSshCommand
    {
        private SshClient client;

        public IdracSshCommand(SshClient client)
        {
            this.client = client;
        }

        public SshCommand UpdateFirmware(string firmwarePath, Repository repository, bool reboot)
        {
            switch (repository.Type)
            {
                case RepositoryType.NFS:
                    return UpdateFirmwareFromNFS(firmwarePath, repository, reboot);
                case RepositoryType.FTP:
                    return UpdateFirmwareFromFTP(firmwarePath, repository, reboot);
                case RepositoryType.TFTP:
                    return UpdateFirmwareFromTFTP(firmwarePath, repository, reboot);
                default:
                    throw new Exception("Tipo invalido de repositório informado");
            }
        }

        private SshCommand UpdateFirmwareFromTFTP(string firmwarePath, Repository repository,  bool reboot)
        {
            string rebootAfter = reboot ? "TRUE" : "FALSE";
            string command = "";
            SshCommand commandResult;
            if (repository.IsPasswordProtected)
            {
                command = String.Format("racadm update -f {0} -e {1} -u {2} -p {3} -a {4} -t TFTP", firmwarePath, repository.Address, repository.User, repository.Password, rebootAfter);
            }
            else
            {
                command = String.Format("racadm update -f {0} -e {1} -a {2} -t TFTP", firmwarePath, repository.Address, rebootAfter);
            }
            commandResult = client.RunCommand(command);
            if(commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }

        private SshCommand UpdateFirmwareFromNFS(string firmwarePath, Repository repository, bool reboot)
        {
            string command = "";
            string rebootAfter = reboot ? "TRUE" : "FALSE";
            SshCommand commandResult;
            if (repository.IsPasswordProtected)
            {
                command = String.Format("racadm update -f {0} -l {1} -u {2} -p {3} -t NFS -a {4}", firmwarePath, repository.Address, repository.User, repository.Password, rebootAfter);
            }
            else
            {
                command = String.Format("racadm update -f {0} -l {1} -t NFS -a {2}", firmwarePath, repository.Address, rebootAfter);
            }
            commandResult = client.RunCommand(command);
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }

        private SshCommand UpdateFirmwareFromFTP(string firmwarePath, Repository repository, bool reboot)
        {
            string rebootAfter = reboot ? "TRUE" : "FALSE";
            string command = "";
            SshCommand commandResult;
            if (repository.IsPasswordProtected)
            {
                command = String.Format("racadm update -f {0} -e {1} -u {2} -p {3} -a {4} -t FTP", firmwarePath, repository.Address, repository.User, repository.Password, rebootAfter);
            }
            else
            {
                command = String.Format("racadm update -f {0} -e {1} -a {2} -t FTP", firmwarePath, repository.Address, rebootAfter);
            }
            commandResult = client.RunCommand(command);
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }

        public SshCommand GenerateUpdateReport(string catalogFile, Repository repository)
        {
            switch (repository.Type)
            {
                case RepositoryType.NFS:
                    return GenerateReportFromNFS(catalogFile, repository);
                case RepositoryType.FTP:
                    return GenerateReportFromFTP(catalogFile, repository);
                case RepositoryType.TFTP:
                    return GenerateReportFromTFTP(catalogFile, repository);
                default:
                    throw new Exception("Tipo invalido de repositório informado");
            }
        }

        private SshCommand GenerateReportFromNFS(string catalogFile, Repository repository)
        {
            string command = "";
            SshCommand commandResult;
            if (repository.IsPasswordProtected)
            {
                command = String.Format("racadm update -f {0} -l {1} -u {2} -p {3} -t NFS -a FALSE --verifycatalog",catalogFile, repository.Address, repository.User, repository.Password);
            }
            else
            {
                command = String.Format("racadm update -f {0} -l {1} -t NFS -a FALSE --verifycatalog", catalogFile, repository.Address);
            }
            commandResult = client.RunCommand(command);
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            commandResult = client.RunCommand("racadm update viewreport");
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }

        private SshCommand GenerateReportFromFTP(string catalogFile, Repository repository)
        {
            string command = "";
            SshCommand commandResult;
            if (repository.IsPasswordProtected)
            {
                command = String.Format("racadm update -f {0} -t FTP -e {1} -u {2} -p {3} -a FALSE --verifycatalog", catalogFile, repository.Address, repository.User, repository.Password);
            }
            else
            {
                command = String.Format("racadm update -f {0} -t FTP -e {1} -a FALSE --verifycatalog", catalogFile, repository.Address);
            }
            commandResult = client.RunCommand(command);
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            commandResult = client.RunCommand("racadm update viewreport");
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }

        private SshCommand GenerateReportFromTFTP(string catalogFile, Repository repository)
        {
            string command = "";
            SshCommand commandResult;
            if (repository.IsPasswordProtected)
            {
                command = String.Format("racadm update -f {0} -t TFTP -e {1} -u {2} -p {3} -a FALSE --verifycatalog", catalogFile, repository.Address, repository.User, repository.Password);
            }
            else
            {
                command = String.Format("racadm update -f {0} -t TFTP -e {1} -a FALSE --verifycatalog", catalogFile, repository.Address);
            }
            commandResult = client.RunCommand(command);
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            commandResult = client.RunCommand("racadm update viewreport");
            if (commandResult.ExitStatus != 0 | commandResult.Result.Contains("ERROR"))
            {
                throw new RacadmException(commandResult.Result);
            }
            return commandResult;
        }

    }
}