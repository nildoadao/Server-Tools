using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Control
{
    class IdracSshCommand
    {
        private SshClient client;

        public IdracSshCommand(SshClient client)
        {
            this.client = client;
        }

        public SshCommand UpdateFirmware(string firmwarePath, string repositoryAddress, RepositoryType type, bool reboot)
        {
            switch (type)
            {
                case RepositoryType.NFS_Share:
                    return UpdateFirmwareFromNfs(firmwarePath, repositoryAddress, reboot);
                case RepositoryType.FTP:
                    return UpdateFirmwareFromFtp(firmwarePath, repositoryAddress, reboot);
                case RepositoryType.TFTP:
                    return UpdateFirmwareFromTftp(firmwarePath, repositoryAddress, reboot);
                default:
                    throw new Exception("Tipo invalido de repositório informado");
            }
        }

        private SshCommand UpdateFirmwareFromTftp(string firmwarePath, string repositoryAddress, bool reboot)
        {
            string rebootAfter = reboot ? "TRUE" : "FALSE";
            string command = String.Format("racadm update -f {0} -e {1} -a {2} -t TFTP", firmwarePath, repositoryAddress, rebootAfter);
            return client.RunCommand(command);
        }

        private SshCommand UpdateFirmwareFromNfs(string firmwarePath, string repositoryAddress, bool reboot)
        {
            string command = String.Format("racadm update -f {0} -l {1}", firmwarePath, repositoryAddress);
            return client.RunCommand(command);
        }

        private SshCommand UpdateFirmwareFromFtp(string firmwarePath, string repositoryAddress, bool reboot)
        {
            string rebootAfter = reboot ? "TRUE" : "FALSE";
            string command = String.Format("racadm update -f {0} -e {1} -a {2} -t FTP", firmwarePath, repositoryAddress, rebootAfter);
            return client.RunCommand(command);
        }

        public SshCommand GenerateUpdateReport(string catalogFile, string repositoryAddress, RepositoryType type)
        {
            switch (type)
            {
                case RepositoryType.NFS_Share:
                    return GenerateReportFromNFS(catalogFile, repositoryAddress);
                case RepositoryType.FTP:
                    return GenerateReportFromFtp(catalogFile, repositoryAddress);
                case RepositoryType.TFTP:
                    return GenerateReportFromTftp(catalogFile, repositoryAddress);
                default:
                    throw new Exception("Tipo invalido de repositório informado");
            }
        }

        private SshCommand GenerateReportFromFtp(string catalogFile, string repositoryAddress)
        {
            string command = String.Format("racadm -f {0} -t FTP -e {1} -a FALSE --verifycatalog", catalogFile, repositoryAddress);
            client.RunCommand(command);
            return client.RunCommand("racadm update viewreport");
        }

        private SshCommand GenerateReportFromTftp(string catalogFile, string repositoryAddress)
        {
            string command = String.Format("racadm -f {0} -t TFTP -e {1} -a FALSE --verifycatalog", catalogFile, repositoryAddress);
            client.RunCommand(command);
            return client.RunCommand("racadm update viewreport");
        }

        private SshCommand GenerateReportFromNFS(string catalogFile, string repositoryAddress)
        {
            string command = String.Format("racadm -f {0} -l {1} -t NFS -a FALSE --verifycatalog", catalogFile, repositoryAddress);
            client.RunCommand(command);
            return client.RunCommand("racadm update viewreport");
        }
    }
}