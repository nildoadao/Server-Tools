using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Server_Tools
{
    class IdracUpdateController
    {
        private SshClient client;

        public SshClient GetClient()
        {
            return client;
        }

        public void SetClient(SshClient client)
        {
            this.client = client;
        }

        public IdracUpdateController(SshClient client)
        {
            this.client = client;
        }

        public IEnumerable<string> ReadReportFile(string path)
        {
            return File.ReadAllLines(path);
        }

        public IEnumerable<IdracFirmware> ReadReportFile(IEnumerable<string> report)
        {
            List<IdracFirmware> reportList = new List<IdracFirmware>();

            string firmwareName = "";
            string avaliableVersion = "";
            string currentVersion = "";

            foreach (string line in report)
            {
                if (!line.Contains("---"))
                {
                    string[] property = line.Split('=');
                    if (String.Equals(property[0].Trim(), "ElementName"))
                    {
                        firmwareName = property[1].Trim();
                    }
                    else if (String.Equals(property[0].Trim(), "Available Version"))
                    {
                        avaliableVersion = property[1].Trim();
                    }
                    else if (String.Equals(property[0].Trim(), "Current Version"))
                    {
                        currentVersion = property[1].Trim();
                    }
                }
                else
                {
                    reportList.Add(new IdracFirmware(false, firmwareName) { AvaliableVersion = avaliableVersion, CurrentVersion = currentVersion });
                }
            }
            return reportList;
        }

        public IEnumerable<IdracFirmware> CompareServerToCatalog(IEnumerable<IdracFirmware> catalogItems, IEnumerable<IdracFirmware> serverItems)
        {
            List<IdracFirmware> updatesAvaliable = new List<IdracFirmware>();

            var updateItem = from catalogItem in catalogItems
                             join serverItem in serverItems on catalogItem.AvaliableVersion equals serverItem.AvaliableVersion                            
                             select new IdracFirmware(false, serverItem.Firmware) { CurrentVersion = serverItem.CurrentVersion, AvaliableVersion = serverItem.AvaliableVersion, FirmwarePath = catalogItem.FirmwarePath };

            foreach(IdracFirmware item in updateItem)
            {
                updatesAvaliable.Add(item);
            }
            return updatesAvaliable;
        }

        public IEnumerable<IdracFirmware> GetCatalogItems(string path)
        {
            List<IdracFirmware> firmwareList = new List<IdracFirmware>();
            XDocument xml = XDocument.Load(path);
            var catalogItems = from c in xml.Root.Descendants("SoftwareComponent")
                               select new IdracFirmware(false, (string)c.Element("Name")){ AvaliableVersion = (string)c.Attribute("vendorVersion"),
                                                                                                FirmwarePath = (string)c.Attribute("path") };

            foreach (IdracFirmware item in catalogItems)
            {
                firmwareList.Add(item);
            }
            return firmwareList;
        }

        public IEnumerable<string> GetUpdateReport(string catalogFile, string nfsShare)
        {
            IdracSshCommand command = new IdracSshCommand();            
            SshCommand commandResult = command.GenerateUpdateReport(client, catalogFile, nfsShare);
            List<string> reportLines = new List<string>();

            string[] lines = commandResult.Result.Split('\n');

            foreach(string line in lines)
            {
                reportLines.Add(line);
            }
            return reportLines;
        }

        public void UpdateFirmware(string firmwareFile, string nfsShare)
        {
            IdracSshCommand idrac = new IdracSshCommand();
            idrac.UpdateFirmware(client, firmwareFile, nfsShare);
        }

        public void UpdateFirmware(IEnumerable<string> firmwareList, string nfsShare)
        {
            IdracSshCommand idrac = new IdracSshCommand();
            foreach (string firmware in firmwareList)
            {
                idrac.UpdateFirmware(client, firmware, nfsShare);
            }                
        }

        public IEnumerable<IdracFirmware> GetUpdates(string repository, string catalogFile)
        {
            //IEnumerable<string> reportResult = GetUpdateReport(catalogFile, repository);
            //var catalogItems = GetCatalogItems(repository + @"\" + catalogFile);

            IEnumerable<string> reportResult = ReadReportFile(@"C:\Users\nildo\Desktop\result.txt");
            var catalogItems = GetCatalogItems(@"C:\Users\nildo\Desktop\Catalog.xml");

            var serverItems = ReadReportFile(reportResult);
            return CompareServerToCatalog(catalogItems, serverItems);
        }
    }
}
