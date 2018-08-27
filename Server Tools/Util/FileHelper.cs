using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using LumenWorks.Framework.IO.Csv;
using Server_Tools.Model;

namespace Server_Tools.Util
{
    class FileHelper
    {
        public static XmlDocument ReadXmlFile(string path)
        {
            XmlDocument file = new XmlDocument();
            file.Load(path);
            return file;
        }

        public static IEnumerable<string> ReadFtpFile(string ftpUri)
        {
            string file = "";

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential("anonymous", "anonymous@anonymous.com");
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                file = reader.ReadToEnd();
            }
            List<string> fileLines = new List<string>();

            foreach (string line in file.Split('\n'))
            {
                fileLines.Add(line);
            }
            return fileLines;
        }

        public static XDocument ReadXmlFtpFile(string ftpUri)
        {
            XDocument file;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential("anonymous", "anonymous@anonymous.com");
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            using (Stream responseStream = response.GetResponseStream())
            {
                file = XDocument.Load(responseStream);
            }

            return file;
        }

        public static IEnumerable<Server> ReadCsvFile(string filePath)
        {
            List<Server> servers = new List<Server>();
            
            using (CsvReader csv = new CsvReader(new StreamReader(filePath), false, ';'))
            {
                while (csv.ReadNextRecord())
                {
                    servers.Add(new Server(csv[0], csv[1], csv[2]));
                }
            }
            return servers;
        }

        public static IEnumerable<string> ReadTxtFile(string filePath)
        {
            return File.ReadLines(filePath);
        }
    }
}
