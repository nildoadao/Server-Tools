using Newtonsoft.Json;
using Server_Tools.Idrac.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class ScpController : BaseIdrac
    {
        // Uris para execução dos Import / Export
        public const string ExportSystemConfiguration = @"/redfish/v1/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ExportSystemConfiguration";
        public const string ImportSystemConfiguration = @"/redfish/v1/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ImportSystemConfiguration";

        /// <summary>
        /// Cria uma nova instancia de ScpController
        /// </summary>
        /// <param name="server">Obejto contendo os dados basicos do servidor</param>
        public ScpController(Server server)
            :base(server)
        { }

        /// <summary>
        /// Exporta um Server Configuration Profile para um servidor que suporta Redfish
        /// </summary>
        /// <param name="target">Parametros que serão incluidos no arquivo</param>
        /// <returns></returns>
        public async Task<IdracJob> ExportScpFileAsync(string target, string exportUse)
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
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(BaseUri + ExportSystemConfiguration, httpContent);
        }

        /// <summary>
        /// Retorna os dados obtidos de um export de arquivo SCP
        /// </summary>
        /// <param name="jobId">Identificação do Job em que foi criado o SCP</param>
        /// <returns>Dados do arquivo</returns>
        public async Task<string> GetScpFileDataAsync(string jobId)
        {
            var idrac = new JobController(Server);
            using (var response = await idrac.GetJobDataAsync(jobId))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException("Falha ao receber dados do Export: " + response.RequestMessage);

                return await response.Content.ReadAsStringAsync();
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
        public async Task<IdracJob> ImportScpFileAsync(string file, string target, string shutdown, string status)
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
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(BaseUri + ImportSystemConfiguration, httpContent);
        }
    }
}
