using Renci.SshNet;
using Server_Tools.Model;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Server_Tools.Control
{
    class IdracUpdateController
    {
        private SshClient client;

        public IdracUpdateController(SshClient client)
        {
            this.client = client;
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
                             select new IdracFirmware(false, serverItem.Firmware)
                             {
                                 CurrentVersion = serverItem.CurrentVersion,
                                 AvaliableVersion = serverItem.AvaliableVersion,
                                 FirmwarePath = catalogItem.FirmwarePath
                             };

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
            var catalogItems = from node in xml.Root.Descendants("SoftwareComponent")
                               select new IdracFirmware(false, (string)node.Element("Name"))
                               {
                                   AvaliableVersion = (string)node.Attribute("vendorVersion"),
                                   FirmwarePath = (string)node.Attribute("path")
                               };

            foreach (IdracFirmware item in catalogItems)
            {
                firmwareList.Add(item);
            }
            return firmwareList;
        }

        public IEnumerable<IdracFirmware> GetCatalogItems(XDocument xmlFile)
        {
            List<IdracFirmware> firmwareList = new List<IdracFirmware>();
            var catalogItems = from node in xmlFile.Root.Descendants("SoftwareComponent")
                               select new IdracFirmware(false, (string)node.Element("Name"))
                               {
                                   AvaliableVersion = (string)node.Attribute("vendorVersion"),
                                   FirmwarePath = (string)node.Attribute("path")
                               };

            foreach (IdracFirmware item in catalogItems)
            {
                firmwareList.Add(item);
            }
            return firmwareList;
        }

        public IEnumerable<string> GetUpdateReport(string catalogFile, string repositoryAddress, RepositoryType type)
        {
            IdracSshCommand command = new IdracSshCommand(client);            
            SshCommand commandResult = command.GenerateUpdateReport(catalogFile, repositoryAddress, type);
            List<string> reportLines = new List<string>();

            foreach(string line in commandResult.Result.Split('\n'))
            {
                reportLines.Add(line);
            }
            return reportLines;
        }

        public void UpdateFirmware(string firmwarePath, string repositoryAddress, RepositoryType type, bool reboot)
        {
            IdracSshCommand idrac = new IdracSshCommand(client);
            idrac.UpdateFirmware(firmwarePath, repositoryAddress, type, reboot);
        }

        public IEnumerable<IdracFirmware> GetUpdates(string catalogFile, string repositoryAddress, RepositoryType type)
        {
            IEnumerable<string> reportResult = GetUpdateReport(catalogFile, repositoryAddress, type);
            var catalogItems = GetCatalogItems(FileHelper.ReadXmlFtpFile(repositoryAddress + "/" + catalogFile));
            var serverItems = ReadReportFile(reportResult);
            return CompareServerToCatalog(catalogItems, serverItems);
        }
    }
}
