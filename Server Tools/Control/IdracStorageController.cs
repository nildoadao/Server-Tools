using Renci.SshNet;
using Server_Tools.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server_Tools.Control
{
    class IdracStorageController
    {
        private Server server;

        public IdracStorageController(Server server)
        {
            this.server = server;
        }

        public IEnumerable<IdracPhysicalDisk> GetPhysicalDisks()
        {
            List<IdracPhysicalDisk> disks = new List<IdracPhysicalDisk>();
            using (SshClient client = new SshClient(server.Host, server.User, server.Password))
            {
                client.Connect();
                IdracSshCommand idrac = new IdracSshCommand(client);
                SshCommand commandResult = idrac.RunCommand("racadm storage get pdisks");
                foreach(string disk in commandResult.Result.Split('\n'))
                {
                    if (!disk.Trim().Equals(""))
                    {
                        string size = GetDiskProperty(disk, "Size");

                        disks.Add(new IdracPhysicalDisk(disk, size)
                        {
                            UsedRaidSpace = GetDiskProperty(disk, "UsedRaidDiskSpace"),
                            FreeRaidSpace = GetDiskProperty(disk, "AvailableRaidDiskSpace"),
                            IsAssigned = IsUnassigned(disk)
                        });
                    }                       
                }
            }
            return disks;
        }

        private string GetDiskProperty(string diskName, string property)
        {
            string result = "";
            using (SshClient client = new SshClient(server.Host, server.User, server.Password))
            {
                client.Connect();
                IdracSshCommand idrac = new IdracSshCommand(client);
                string command = String.Format("racadm storage get pdisks --refkey {0} -o - p {1}", diskName, property);
                result = idrac.RunCommand(command).Result;
            }
            return result;
        }

        public IEnumerable<string> GetVirtuallDisks()
        {
            List<string> vDisks = new List<string>();
            using (SshClient client = new SshClient(server.Host, server.User, server.Password))
            {
                client.Connect();
                IdracSshCommand idrac = new IdracSshCommand(client);
                SshCommand commandResult = idrac.RunCommand("racadm storage get vdisks");
                foreach (string vdisk in commandResult.Result.Split('\n'))
                {
                    vDisks.Add(vdisk);
                }
            }
            return vDisks;
        }

        public bool IsUnassigned(string diskName)
        {
            bool unassigned = true;

            using (SshClient client = new SshClient(server.Host, server.User, server.Password))
            {
                client.Connect();
                IdracSshCommand idrac = new IdracSshCommand(client);
                string command = String.Format("racadm storage get pdisks --refkey {0} -o - p AvailableraidDiskSpace", diskName);
                SshCommand commandResult = idrac.RunCommand(command);
                if(commandResult.Result.Trim().Equals("0.00 GB"))
                {
                    unassigned = false;
                }
            }
            return unassigned;
        }
    }
}
