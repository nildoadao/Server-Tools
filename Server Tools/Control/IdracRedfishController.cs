using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server_Tools.Model;
using Server_Tools.Util;
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

        #region Redfish URLs

        public const string REDFISH_ROOT = @"/redfish/v1";
        public const string FIRMWARE_INVENTORY = @"/UpdateService/FirmwareInventory/";
        public const string FIRMWARE_INSTALL = @"/UpdateService/Actions/Oem/DellUpdateService.Install";
        public const string JOB_RESULT = @"/Managers/iDRAC.Embedded.1/Jobs/";
        public const string JOB_STATUS = @"/TaskService/Tasks/";
        public const string EXPORT_SYSTEM_CONFIGURATION = @"Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ExportSystemConfiguration";

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

        private async Task<HttpResponseMessage> UploadFirmwareToIdrac(string firmwarePath)
        {
            HttpClient client = HttpUtil.GetClient();
            var content = new MultipartFormDataContent(Guid.NewGuid().ToString());
            var firmwareFile = File.ReadAllBytes(firmwarePath);
            content.Add(new StreamContent(new MemoryStream(firmwareFile)), "Firmware", Path.GetFileName(firmwarePath));
            return await client.PostAsync(baseUri + FIRMWARE_INVENTORY, content);
        }

        private async Task<HttpResponseMessage> InstallFirmware(string location, IdracInstallOption option)
        {
            var content = new
            {
                SoftwareIdentityURIs = location,
                InstallUpon = option
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpClient client = HttpUtil.GetClient();
            return await client.PostAsync(baseUri + FIRMWARE_INSTALL, httpContent);
        }

        public async Task<string> UpdateIdracFirmware(string firmwarePath, IdracInstallOption option)
        {
            bool support = await CheckRedfishSupport(FIRMWARE_INVENTORY);
            if (!support)
            {
                throw new NotSupportedException("Host não suporta o recurso solicitado");
            }
            string location = "";
            using (HttpResponseMessage response = await UploadFirmwareToIdrac(firmwarePath))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha no upload do firmware: " + response.RequestMessage);
                }
                List<string> locations = (List<string>) response.Headers.GetValues("Location");
                location = locations[0];
            }
            using (HttpResponseMessage response = await InstallFirmware(location, option))
            {
                if (!response.IsSuccessStatusCode)
                {
                        throw new HttpRequestException("Falha ao instalar o firmware: " + response.RequestMessage);
                }
            }
            return "Job Criado com sucesso !";
        }

        public async Task<string> ExportScpFile(IdracScpTarget target)
        {
            bool support = await CheckRedfishSupport(EXPORT_SYSTEM_CONFIGURATION);

            if (!support)
            {
                throw new NotSupportedException("Host não suporta o recurso solicitado");
            }

            var content = new
            {
                ExportFormat = "XML",
                ShareParameters = new
                {
                    Target = target
                }
            };

            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            string jobId = "";

            using(HttpResponseMessage response = await HttpUtil.GetClient().PostAsync(baseUri + EXPORT_SYSTEM_CONFIGURATION, httpContent))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha ao criar Job: " + response.RequestMessage);
                }
                jobId = Regex.Match(response.Content.ToString(), "JID_.+?r").Captures[0].Value.Replace("\r", "");
            }

            var startTime = DateTime.Now;
            bool jobOk = false;

            while (!jobOk) // Aguarda o Job ser concluido, Timeout de 5 minutos
            {
                using (HttpResponseMessage response = await HttpUtil.GetClient().GetAsync(baseUri + JOB_RESULT + jobId))
                {
                    var exportJob = JsonConvert.DeserializeObject<IdracJob>(response.Content.ToString());
                    if (exportJob.TaskState.Equals("Failed"))
                    {
                        throw new HttpRequestException("Falha ao executar Job: " + response.RequestMessage);
                    }
                    else if (startTime.AddMinutes(5) >= DateTime.Now)
                    {
                        throw new TimeoutException("Execedido tempo para execução do JOB" + response.RequestMessage);
                    }
                    else if (exportJob.TaskState.Equals("Completed"))
                    {
                        jobOk = true;
                    }
                }
            }

            // Le os dados do Job e salva em um arquivo .xml
            using (HttpResponseMessage response = await HttpUtil.GetClient().GetAsync(baseUri + JOB_RESULT + jobId))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha ao receber dados do Export: " + response.RequestMessage);
                }
                var jobData = response.Content.ToString();
                File.WriteAllText(DateTime.Now.ToString() + ".xml", jobData);
            }

            return "Arquivo exportado com sucesso !";
        }
    }
}
