using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server_Tools.Model;
using Server_Tools.Util;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Server_Tools.Control
{
    class IdracRedfishController
    {
        Server server;
        string baseUri;
        HttpClient client;
        const double JOB_TIMEOUT = 10; // Timeout de 5 minutos para conclusão dos Jobs

        #region Redfish URLs

        public const string REDFISH_ROOT = @"/redfish/v1";
        public const string FIRMWARE_INVENTORY = @"/redfish/v1/UpdateService/FirmwareInventory";
        public const string FIRMWARE_INSTALL = @"/redfish/v1/UpdateService/Actions/Oem/DellUpdateService.Install";
        public const string JOB_STATUS = @"/redfish/v1/Managers/iDRAC.Embedded.1/Jobs/";
        public const string JOB_RESULT = @"/redfish/v1/TaskService/Tasks/";
        public const string EXPORT_SYSTEM_CONFIGURATION = @"/redfish/v1/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ExportSystemConfiguration";
        public const string IMPORT_SYSTEM_CONFIGURATION = @"/redfish/v1/Managers/iDRAC.Embedded.1/Actions/Oem/EID_674_Manager.ImportSystemConfiguration";

        #endregion

        /// <summary>
        /// Inicia uma nova classe IdracRedfishController
        /// </summary>
        /// <param name="server">Classe com os dados da Idrac</param>
        public IdracRedfishController(Server server)
        {
            this.server = server;
            baseUri = String.Format(@"https://{0}", server.Host);
            client = HttpUtil.Client;
        }

        /// <summary>
        /// Checa se um determinado recurso da API está disponivel
        /// </summary>
        /// <param name="resource">caminho do recurso desejado ex: /UpdateService/FirmwareInventory</param>
        /// <returns></returns>
        public async Task<bool> CheckRedfishSupport(string resource)
        {
            bool support = false;
            using (var request = new HttpRequestMessage(HttpMethod.Get, resource))          
            {
                request.Headers.Authorization = HttpUtil.GetCredentialHeader(server.User, server.Password);
                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                        support = true;
                }
                return support;
            }
        }

        /// <summary>
        /// Realiza um update de firmware em um servidor que suporta Redfish
        /// </summary>
        /// <param name="path">Caminho completo para o arquvi do firmware</param>
        /// <param name="option">Modo de instalação</param>
        /// <returns>IdracJob</returns>
        public async Task<IdracJob> UpdateFirmware(string path, string option)
        {
            Uri location = await UploadFile(path);
            return await InstallFirmware(location, option);
        }

        /// <summary>
        /// Realiza a instalação do Firmware na Idrac
        /// </summary>
        /// <param name="location">Uri do recurso</param>
        /// <param name="option">Modo de Instalação</param>
        /// <returns>Job de Instação fo Firmware</returns>
        private async Task<IdracJob> InstallFirmware(Uri location, string option)
        {
            var content = new
            {
                SoftwareIdentityURIs = location,
                InstallUpon = option
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");              
            string jobId = await CreateJob(baseUri + FIRMWARE_INSTALL, httpContent);
            return await GetJob(jobId);            
        }

        /// <summary>
        /// Realiza o upload do firmware para a Idrac
        /// </summary>
        /// <param name="path">caminho completo do arquivo do firmware</param>
        /// <returns>Uri com a localização do recurso</returns>
        private async Task<Uri> UploadFile(string path)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, baseUri + FIRMWARE_INVENTORY))
            using (var content = new MultipartFormDataContent(Guid.NewGuid().ToString()))
            using (var fileContent = new StreamContent(File.Open(path, FileMode.Open)))
            {
                string etag = await GetHeaderValue("ETag", baseUri + FIRMWARE_INVENTORY);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                request.Headers.Authorization = HttpUtil.GetCredentialHeader(server.User, server.Password);
                request.Headers.TryAddWithoutValidation("If-Match", etag);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = Path.GetFileName(path).ToLower()
                };
                content.Add(fileContent);
                request.Content = content;
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException("Falha no upload do arquivo: " + response.ReasonPhrase);
                    }
                    return response.Headers.Location;
                }
            }
        }

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
            string jobId = await CreateJob(baseUri + EXPORT_SYSTEM_CONFIGURATION, httpContent);
            DateTime startTime = DateTime.Now;

            while (true)
            {
                var job = await GetJob(jobId);
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
            using (var request = new HttpRequestMessage(HttpMethod.Get, baseUri + JOB_RESULT + jobId))
            {
                request.Headers.Authorization = HttpUtil.GetCredentialHeader(server.User, server.Password);
                using(var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException("Falha ao receber dados do Export: " + response.RequestMessage);
                    }
                    string jobData = await response.Content.ReadAsStringAsync();
                    string currentTime = DateTime.Now.ToString().Replace(":", "").Replace("/", "").Replace(" ", "");
                    string dowloadsFolder = KnownFolders.Downloads.Path;
                    string path = Path.Combine(dowloadsFolder, "SCP_" + currentTime + ".xml");
                    File.WriteAllText(path, jobData);
                    return path;
                }
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
            string jobId = await CreateJob(baseUri + IMPORT_SYSTEM_CONFIGURATION, httpContent);
            return await GetJob(jobId);
        }

        /// <summary>
        /// Cria um Job na Idrac
        /// </summary>
        /// <param name="uri">String contento o enderço do recurso</param>
        /// <param name="content">Conteudo Http da requisição</param>
        /// <returns></returns>
        public async Task<string> CreateJob(string uri, HttpContent content)
        {
            using(var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Authorization = HttpUtil.GetCredentialHeader(server.User, server.Password);
                request.Content = content;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException("Falha ao criar Job: " + response.Content);
                    }
                    return Regex.Match(response.Headers.Location.ToString(), "JID_.*").Captures[0].Value.Replace("\r", "");
                }
            }
        }

        /// <summary>
        /// Retorna um objeto contendo os dados do Job da Idrac
        /// </summary>
        /// <param name="jobId">Identificação do Job</param>
        /// <returns>O Job corresponde ao ID</returns>
        public async Task<IdracJob> GetJob(string jobId)
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
            using(var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Authorization = HttpUtil.GetCredentialHeader(server.User, server.Password);
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException(string.Format("Falha ao obter recurso {0} {1}", uri, response.ReasonPhrase));
                    }
                    string jsonBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(jsonBody);
                }
            }
        
        }

        /// <summary>
        /// Retorna o valor de um cabeçalho especifico
        /// </summary>
        /// <param name="header">Cabeçalho a ser buscado</param>
        /// <param name="uri">Uri do recurso</param>
        /// <returns>O valor de cabeçalho</returns>
        public async Task<string> GetHeaderValue(string header, string uri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Authorization = HttpUtil.GetCredentialHeader(server.User, server.Password);
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao obter o cabeçalho {0} {1}", header, response.ReasonPhrase));

                    string result = "";
                    foreach(string item in response.Headers.GetValues(header))
                    {
                        result = item;
                    }
                    return result;
                }
            }
        }
    }
}
