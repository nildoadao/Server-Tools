using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Server_Tools
{
    class FileHelper
    {
        public static XmlDocument ReadXmlFile(string path)
        {
            XmlDocument file = new XmlDocument();
            file.Load(path);
            return file;
        } 
    }
}
