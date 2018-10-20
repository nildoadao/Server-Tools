using Newtonsoft.Json;
using Server_Tools.Idrac.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class UpdateController : BaseIdrac
    {

        public const string FirmwareInventory = @"/redfish/v1/UpdateService/FirmwareInventory";
        public const string FirmwareInstall = @"/redfish/v1/UpdateService/Actions/Oem/DellUpdateService.Install";

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
            string jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            return await idrac.CreateJob(baseUri + FirmwareInstall, httpContent);
        }

        /// <summary>
        /// Realiza o upload do firmware para a Idrac
        /// </summary>
        /// <param name="path">caminho completo do arquivo do firmware</param>
        /// <returns>Uri com a localização do recurso</returns>
        private async Task<Uri> UploadFile(string path)
        {
            var encoding = Encoding.GetEncoding("ISO-8859-1");
            string decodedFile = encoding.GetString(File.ReadAllBytes(path));
            using (var request = new HttpRequestMessage(HttpMethod.Post, baseUri + FirmwareInventory))
            using (var multipartContent = new MultipartFormDataContent())
            using (var fileContent = new StringContent(decodedFile))
            {
                string etag = await GetHeaderValue("ETag", baseUri + FirmwareInventory);
                request.Headers.Authorization = credentials;
                request.Headers.TryAddWithoutValidation("If-Match", etag);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = Path.GetFileName(path).ToLower()
                };
                multipartContent.Add(fileContent);
                request.Content = multipartContent;
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
