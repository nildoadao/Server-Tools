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

        private async Task<HttpResponseMessage> InstallFirmware(HttpResponseMessage uploadResponse, IdracInstallOption option)
        {
            var headers = uploadResponse.Headers.GetValues("Location");
            var content = new
            {
                SoftwareIdentityURIs = headers,
                InstallUpon = option
            };

            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpClient client = HttpUtil.GetClient();
            return await client.PostAsync(baseUri + FIRMWARE_INSTALL, httpContent);
        }

        public async Task<string> UploadIdracFirmware(string firmwarePath, IdracInstallOption option)
        {
            bool support = await CheckRedfishSupport(FIRMWARE_INVENTORY);

            if (!support)
            {
                throw new NotSupportedException("Host não suporta o recurso solicitado");
            }

            using (HttpResponseMessage uploadResponse = await UploadFirmwareToIdrac(firmwarePath))
            {
                if (!uploadResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Falha no upload do firmware: " + uploadResponse.RequestMessage);
                }
                using (HttpResponseMessage installResponse = await InstallFirmware(uploadResponse, option))
                {
                    if (!installResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException("Falha ao instalar o firmware: " + installResponse.RequestMessage);
                    }
                }
            }
            return "Job Criado com sucesso !";
        }
    }
}
