using Newtonsoft.Json;
using Server_Tools.Model;
using Syroot.Windows.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac
{
    class ScpController : BaseIdrac
    {

        public const string EXPORT_SYSTEM_CONFIGURATION = @"/redfish/v1/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ExportSystemConfiguration";
        public const string IMPORT_SYSTEM_CONFIGURATION = @"/redfish/v1/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ImportSystemConfiguration";
        const double JOB_TIMEOUT = 10; // Timeout de 5 minutos para conclusão dos Jobs

        public ScpController(Server server)
            :base(server)
        { }

        /// <summary>
        /// Exporta um Server Configuration Profile para um servidor que suporta Redfish
        /// </summary>
        /// <param name="target">Parametros que serão incluidos no arquivo</param>
        /// <returns></returns>
        public async Task<string> ExportScpFile(string target, string exportUse)
        {
            var content = new
            {
                ExportFormat = "XML",
                ShareParameters = new
                {
                    Target = target
                },
                ExportUse = exportUse
            };

            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            string jobId = await idrac.CreateJob(baseUri + EXPORT_SYSTEM_CONFIGURATION, httpContent);
            DateTime startTime = DateTime.Now;

            while (true)
            {
                var job = await idrac.GetJob(jobId);
                if (job.JobState.Contains("Completed"))
                {
                    break;
                }
                else if (job.JobState.Equals("Failed"))
                {
                    throw new HttpRequestException("Falha ao executar o Job: " + job.Message);
                }
                if (DateTime.Now >= startTime.AddMinutes(JOB_TIMEOUT))
                {
                    throw new TimeoutException("Excedido tempo para conclusão do Job " + jobId);
                }
            }
            using (var response = await idrac.GetJobData(jobId))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException("Falha ao receber dados do Export: " + response.RequestMessage);

                string jobData = await response.Content.ReadAsStringAsync();
                string currentTime = DateTime.Now.ToString().Replace(":", "").Replace("/", "").Replace(" ", "");
                string dowloadsFolder = KnownFolders.Downloads.Path;
                string path = Path.Combine(dowloadsFolder, "SCP_" + currentTime + ".xml");
                File.WriteAllText(path, jobData);
                return path;               
            }
        }

        /// <summary>
        /// Importa um arquivo do tipo Server Configuration Profile para um servidor com suporte a Redfish
        /// </summary>
        /// <param name="file">Caminho completo do arquvio SCP</param>
        /// <param name="target">Atributos que sofrerão alteração</param>
        /// <param name="shutdown">Método de desligamento, caso necessario</param>
        /// <param name="status">Status do servidor On/Off</param>
        /// <returns></returns>
        public async Task<IdracJob> ImportScpFile(string file, string target, string shutdown, string status)
        {
            string fileLines = File.ReadAllText(file);
            var content = new
            {
                ImportBuffer = fileLines,
                ShareParameters = new
                {
                    Target = target
                },
                ShutdownType = shutdown,
                HostPowerState = status
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            string jobId = await idrac.CreateJob(baseUri + IMPORT_SYSTEM_CONFIGURATION, httpContent);
            return await idrac.GetJob(jobId);
        }
    }
}
