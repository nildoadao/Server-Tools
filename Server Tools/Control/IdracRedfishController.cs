using Newtonsoft.Json;
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
using System.Threading.Tasks;

namespace Server_Tools.Control
{
    class IdracRedfishController
    {
        Server server;
        string baseUri;

        #region Redfish URLs

        const string REDFISH_ROOT = @"/redfish/v1";
        const string FIRMWARE_INVENTORY = @"/UpdateService/FirmwareInventory/";

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

        public async Task<bool> CheckRedfishSupport()
        {
            HttpClient client = HttpUtil.GetClient();
            bool support = false;
            using (var response = await client.GetAsync(baseUri + FIRMWARE_INVENTORY))
            {
                if (response.IsSuccessStatusCode)
                {
                    support = true;
                }
            }
            return support;
        }

        public Task<HttpResponseMessage> UploadFirmwareToIdrac(string firmwarePath)
        {
            HttpClient client = HttpUtil.GetClient();
            var content = new MultipartFormDataContent(Guid.NewGuid().ToString());
            var firmwareFile = File.ReadAllBytes(firmwarePath);
            content.Add(new StreamContent(new MemoryStream(firmwareFile)), "Firmware", Path.GetFileName(firmwarePath));
            return client.PostAsync(baseUri + FIRMWARE_INVENTORY, content);
        }
    }
}
