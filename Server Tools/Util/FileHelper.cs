using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using LumenWorks.Framework.IO.Csv;
using Server_Tools.Idrac.Models;

namespace Server_Tools.Util
{
    class FileHelper
    {
        /// <summary>
        /// Retorna os registros Server em um arquivo CSV
        /// </summary>
        /// <param name="filePath">Localização completa do arquivo</param>
        /// <returns></returns>
        public static IEnumerable<Server> ReadCsvFile(string filePath)
        {         
            using (CsvReader csv = new CsvReader(new StreamReader(filePath), false, ';'))
            {
                while (csv.ReadNextRecord())
                {
                    yield return new Server(csv[0], csv[1], csv[2]);
                }
            }
        }
    }
}
