using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace Server_Tools
{
    class NetworkHelper
    {
        //Realiza um teste de ping para um determinado IP
        public static bool IsConnected(string IpAddres)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(IpAddres);

                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception) // Caso o endereço seja fornecido de maneira errada/ mal formatado
            {
                return false;
            }
        }

        public static IEnumerable<string> ReadFtpFile(string ftpUri)
        {
            string file = "";

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential("anonymous", "nildo@nildo.com");
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
    
            using (Stream responseStream = response.GetResponseStream())
            using(StreamReader reader = new StreamReader(responseStream))
            {
                file = reader.ReadToEnd();
            }
            List<string> fileLines = new List<string>();

            foreach(string line in file.Split('\n'))
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
            request.Credentials = new NetworkCredential("anonymous", "nildo@nildo.com");
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            using (Stream responseStream = response.GetResponseStream())
            { 
                file = XDocument.Load(responseStream);
            }

            return file;
        }
    }
}
