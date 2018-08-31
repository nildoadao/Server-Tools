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
            List<string> disksNames = new List<string>();

            using (SshClient client = new SshClient(server.Host, server.User, server.Password))
            {
                client.Connect();
                IdracSshCommand idrac = new IdracSshCommand(client);
                SshCommand command = idrac.GetPhysicalDisksNames();

                // Carrega uma lista com os nomes dos discos
                foreach(string disk in command.Result.Split('\n'))
                {
                    if (!disk.Trim().Equals(""))
                        disksNames.Add(disk);                     
                }

                command = idrac.GetPhysicalDisksPropreties();
                string currentDisk = "";

                foreach(string line in command.Result.Split('\n'))
                {
                    if (disksNames.Contains(line.Trim()))
                    {
                        disks.Add(new IdracPhysicalDisk(line));
                        currentDisk = line.Trim();
                        continue;
                    }
                    string[] property = line.Split('=');

                    if (property[0].Trim().Equals("Status"))
                    {
                        IdracPhysicalDisk disk = disks.Find(x => x.Name.Equals(currentDisk));
                        disk.Status = property[1].Trim();                      
                    }
                    else if (property[0].Trim().Equals("State"))
                    {
                        IdracPhysicalDisk disk = disks.Find(x => x.Name.Equals(currentDisk));
                        disk.State = property[1].Trim();
                    }
                    else if (property[0].Trim().Equals("Size"))
                    {
                        IdracPhysicalDisk disk = disks.Find(x => x.Name.Equals(currentDisk));
                        disk.Size = property[1].Trim();
                    }
                    else if (property[0].Trim().Equals("UsedRaidDiskSpace"))
                    {
                        IdracPhysicalDisk disk = disks.Find(x => x.Name.Equals(currentDisk));
                        disk.UsedRaidSpace = property[1].Trim();
                    }
                    else if (property[0].Trim().Equals("AvailableRaidDiskSpace"))
                    {
                        IdracPhysicalDisk disk = disks.Find(x => x.Name.Equals(currentDisk));
                        disk.FreeRaidSpace = property[1].Trim();
                        if(disk.FreeRaidSpace.Equals("0.00 GB"))
                        {
                            disk.IsAssigned = true;
                        }
                        else
                        {
                            disk.IsAssigned = false;
                        }
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
                string command = String.Format("racadm storage get pdisks --refkey {0} -o -p {1}", diskName, property);
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
    }
}
