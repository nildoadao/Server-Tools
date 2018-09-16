﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server_Tools.Model;
using Server_Tools.Util;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Server_Tools.Control
{
    class IdracRedfishController
    {
        Server server;
        string baseUri;
        private const double JOB_TIMEOUT = 5; // Timeout de 5 minutos para conclusão dos Jobs

        #region Redfish URLs

        public const string REDFISH_ROOT = @"/redfish/v1";
        public const string FIRMWARE_INVENTORY = @"/UpdateService/FirmwareInventory/";
        public const string FIRMWARE_INSTALL = @"/UpdateService/Actions/Oem/DellUpdateService.Install";
        public const string JOB_STATUS = @"/Managers/iDRAC.Embedded.1/Jobs/";
        public const string JOB_RESULT = @"/TaskService/Tasks/";
        public const string EXPORT_SYSTEM_CONFIGURATION = @"/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ExportSystemConfiguration";
        public const string IMPORT_SYSTEM_CONFIGURATION = @"/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ImportSystemConfiguration";

        #endregion

        /// <summary>
        /// Inicia uma nova classe IdracRedfishController
        /// </summary>
        /// <param name="server">Classe com os dados da Idrac</param>
        public IdracRedfishController(Server server)
        {
            this.server = server;
            baseUri = String.Format(@"https://{0}{1}", server.Host, REDFISH_ROOT);
        }

        /// <summary>
        /// Checa se um determinado recurso da API está disponivel
        /// </summary>
        /// <param name="resource">caminho do recurso desejado ex: /UpdateService/FirmwareInventory</param>
        /// <returns></returns>
        public async Task<bool> CheckRedfishSupport(string resource)
        {
            HttpClient client = HttpUtil.GetClient();
            bool support = false;
            using (var response = await client.GetAsync(baseUri + resource))
            {
                if (response.IsSuccessStatusCode)
                {
                    support = true;
                }
            }
            return support;
        }

        /// <summary>
        /// Realiza um update de firmware em um servidor que suporta Redfish
        /// </summary>
        /// <param name="firmwarePath">Caminho completo para o arquvi do firmware</param>
        /// <param name="option">Modo de instalação</param>
        /// <returns></returns>
        public async Task<string> UpdateFirmware(string firmwarePath, IdracInstallOption option)
        {
            var firmwareContent = new MultipartFormDataContent(Guid.NewGuid().ToString());
            var firmwareFile = File.ReadAllBytes(firmwarePath);
            firmwareContent.Add(new StreamContent(new MemoryStream(firmwareFile)), "Firmware", Path.GetFileName(firmwarePath));
            var location = await UploadFile(baseUri + FIRMWARE_INVENTORY, firmwareContent);
            var content = new
            {
                SoftwareIdentityURIs = location,
                InstallUpon = option
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            string jobId = await CreateJob(baseUri + FIRMWARE_INSTALL, httpContent);
            DateTime startTime = DateTime.Now;

            while (true)
            {
                var job = await GetJob(jobId);
                if (job.JobState.Equals("Completed"))
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
            /* To do
             * Após a Atualização, ler os dados do firmware e retornar um objeto IdracFirmware
             */
            return "Firmware Name";
        }

        /// <summary>
        /// Realiza o upload de um arquivo para um servidor que suporta Redfish
        /// </summary>
        /// <param name="uri">URL do recurso</param>
        /// <param name="content">Conteudo Http contendo o arquivo</param>
        /// <returns>Uri do novo recurso</returns>
        private async Task<Uri> UploadFile(string uri, HttpContent content)
        {
            Uri location;
            using (HttpResponseMessage response = await HttpUtil.GetClient().PostAsync(uri, content))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha no upload do arquivo " + response.ReasonPhrase);
                }
                location = response.Headers.Location;
            }
            return location;
        }

        /// <summary>
        /// Exporta um Server Configuration Profile para um servidor que suporta Redfish
        /// </summary>
        /// <param name="target">Parametros que serão incluidos no arquivo</param>
        /// <returns></returns>
        public async Task<string> ExportScpFile(IdracScpTarget target)
        {
            var content = new
            {
                ExportFormat = "XML",
                ShareParameters = new
                {
                    Target = target.ToString()
                }
            };

            var jsonContent = JsonConvert.SerializeObject(content);            
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            string jobId = await CreateJob(baseUri + EXPORT_SYSTEM_CONFIGURATION, httpContent);
            DateTime startTime = DateTime.Now;

            while (true)
            {
                var job = await GetJob(jobId);
                if (job.JobState.Equals("Completed"))
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
            string path;
            using (HttpResponseMessage response = await HttpUtil.GetClient().GetAsync(baseUri + JOB_RESULT + jobId))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha ao receber dados do Export: " + response.RequestMessage);
                }
                string jobData = await response.Content.ReadAsStringAsync();
                string currentTime = DateTime.Now.ToString().Replace(":", "").Replace("/", "").Replace(" ", "");
                string dowloadsFolder = KnownFolders.Downloads.Path;
                path = Path.Combine(dowloadsFolder, "SCP_" + currentTime + ".xml");
                File.WriteAllText(path, jobData);
            }
            return path;
        }

        /// <summary>
        /// Importa um arquivo do tipo Server Configuration Profile para um servidor com suporte a Redfish
        /// </summary>
        /// <param name="file">Caminho completo do arquvio SCP</param>
        /// <param name="target">Atributos que sofrerão alteração</param>
        /// <param name="shutdown">Método de desligamento, caso necessario</param>
        /// <param name="status">Status do servidor On/Off</param>
        /// <returns></returns>
        public async Task<IdracJob> ImportScpFile(string file, IdracScpTarget target, IdracShutdownType shutdown, IdracHostPowerStatus status)
        {
            string fileLines = File.ReadAllText(file);
            var content = new
            {
                ImportBuffer = fileLines,
                ShareParameters = new
                {
                    Target = target.ToString()
                },
                ShutdownType = shutdown.ToString(),
                HostPowerState = status.ToString()
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            string jobId = await CreateJob(baseUri + IMPORT_SYSTEM_CONFIGURATION, httpContent);
            DateTime startTime = DateTime.Now;
            IdracJob job = new IdracJob();

            while(true)
            {
                job = await GetJob(jobId);
                if (job.JobState.Equals("Completed"))
                {
                    break;
                }
                else if (job.JobState.Equals("Failed"))
                {
                    throw new HttpRequestException("Falha ao executar o Job: " + job.Message);
                }
                else if(job.JobState.Equals("Pending") & shutdown == IdracShutdownType.NoReboot)
                {
                    break;
                }
                if (DateTime.Now >= startTime.AddMinutes(JOB_TIMEOUT))
                {
                    throw new TimeoutException("Excedido tempo para conclusão do Job " + jobId);
                }
            }        
            return job;
        }

        /// <summary>
        /// Cria um Job na Idrac
        /// </summary>
        /// <param name="uri">String contento o enderço do recurso</param>
        /// <param name="content">Conteudo Http da requisição</param>
        /// <returns></returns>
        private async Task<string> CreateJob(string uri, HttpContent content)
        {
            string jobId = "";
            using (HttpResponseMessage response = await HttpUtil.GetClient().PostAsync(uri, content))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha ao criar Job: " + response.Content);
                }
                jobId = Regex.Match(response.Headers.Location.ToString(), "JID_.*").Captures[0].Value.Replace("\r", "");
            }
            return jobId;
        }

        /// <summary>
        /// Retorna um objeto contendo os dados do Job da Idrac
        /// </summary>
        /// <param name="jobId">Identificação do Job</param>
        /// <returns>O Job corresponde ao ID</returns>
        private async Task<IdracJob> GetJob(string jobId)
        {
            return await GetResource<IdracJob>(baseUri + JOB_STATUS + jobId);
        }

        /// <summary>
        /// Retorna um objeto genérico a partir de uma Uri da API Redfish
        /// </summary>
        /// <typeparam name="T">Tipo do Objeto de retorno</typeparam>
        /// <param name="uri">Localização do recurso</param>
        /// <returns></returns>
        private async Task<T> GetResource<T>(string uri)
        {
            string jsonBody;
            using (HttpResponseMessage response = await HttpUtil.GetClient().GetAsync(uri))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha ao obter recurso: " + uri + " " + response.ReasonPhrase);
                }
                jsonBody = await response.Content.ReadAsStringAsync();
            }
            return JsonConvert.DeserializeObject<T>(jsonBody);
        }
    }
}
