using Newtonsoft.Json;
using Server_Tools.Model;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac
{
    class UpdateController : BaseIdrac
    {

        public const string FIRMWARE_INVENTORY = @"/redfish/v1/UpdateService/FirmwareInventory";
        public const string FIRMWARE_INSTALL = @"/redfish/v1/UpdateService/Actions/Oem/DellUpdateService.Install";

        public UpdateController(Server server)
            :base(server)
        { }

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
            var idrac = new JobController(server);
            string jobId = await idrac.CreateJob(baseUri + FIRMWARE_INSTALL, httpContent);
            return await idrac.GetJob(jobId);
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
            using (var fileContent = new StreamContent(File.Open(path, FileMode.Open, FileAccess.Read)))
            {
                string etag = await GetHeaderValue("ETag", baseUri + FIRMWARE_INVENTORY);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                request.Headers.Authorization = credentials;
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
                        throw new HttpRequestException("Falha no upload do arquivo: " + response.ReasonPhrase);

                    return response.Headers.Location;
                }
            }
        }
    }
}
