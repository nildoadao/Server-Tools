using Newtonsoft.Json;
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

            while (await CheckJobStatus(jobId))// Aguarda o Job ser concluido, Timeout de 5 minutos
            {
                if(DateTime.Now >= startTime.AddMinutes(5))
                {
                    throw new TimeoutException("Excedido tempo para conclusão do Job " + jobId);
                }
            } 
            string path = await SaveScpFile(jobId);
            return "Arquivo exportado com sucesso !\nArquivo salvo em :" + path;
        }

        public async void ImportScpFile(string file, IdracScpTarget target, IdracShutdownType shutdown, IdracHostPowerStatus status)
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

            while (await CheckJobStatus(jobId))// Aguarda o Job ser concluido, Timeout de 5 minutos
            {
                if (DateTime.Now >= startTime.AddMinutes(5))
                {
                    throw new TimeoutException("Excedido tempo para conclusão do Job " + jobId);
                }
            }
        }

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

        private async Task<bool> CheckJobStatus(string jobId)
        {
            IdracJob job;
            using (HttpResponseMessage response = await HttpUtil.GetClient().GetAsync(baseUri + JOB_STATUS + jobId))
            {
                string jsonBody = await response.Content.ReadAsStringAsync();
                job = JsonConvert.DeserializeObject<IdracJob>(jsonBody);
            }

            if (job.JobState.Equals("Failed"))
            {
                throw new HttpRequestException("Falha ao executar Job: " + job.Message);
            }
            else if (job.JobState.Equals("Completed"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<string> SaveScpFile(string jobId)
        {
            string path;
            using (HttpResponseMessage response = await HttpUtil.GetClient().GetAsync(baseUri + JOB_RESULT + jobId))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha ao receber dados do Export: " + response.RequestMessage);
                }
                string jobData = await response.Content.ReadAsStringAsync();
                string currentTime = DateTime.Now.ToString().Replace(":", "").Replace("/", "");
                string dowloadsFolder = KnownFolders.Downloads.Path;
                path = Path.Combine(dowloadsFolder, "SCP_" + currentTime + ".xml");
                File.WriteAllText(path, jobData);
            }
            return path;
        }
    }
}
