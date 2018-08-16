using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools
{
    class IdracSshCommand
    {
        public SshCommand UpdateFirmware(SshClient client, string firmwareFile, string nfsShare)
        {
            return client.RunCommand("racadm update -f " + firmwareFile + " " + nfsShare + " -t NFS -a FALSE");
        }

        public SshCommand GenerateUpdateReport(SshClient client, string catalogFile, string nfsShare)
        {
            client.RunCommand("racadm update -f " + catalogFile + " -l " + nfsShare + " -t NFS -a FALSE --verifycatalog");
            return client.RunCommand("racadm update viewreport");
        }
    }
}