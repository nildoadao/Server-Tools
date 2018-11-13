using System.Collections.Generic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Server_Tools.Idrac.Models;

namespace Server_Tools.Util
{
    class FileHelper
    {
        /// <summary>
        /// Retorna os registros Server de um arquivo CSV
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
