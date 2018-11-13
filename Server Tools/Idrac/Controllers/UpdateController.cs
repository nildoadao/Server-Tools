using Newtonsoft.Json;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class UpdateController : BaseIdrac
    {
        #region Redfish Uris
        public const string FirmwareInventory = @"/redfish/v1/UpdateService/FirmwareInventory";
        public const string FirmwareInstall = @"/redfish/v1/UpdateService/Actions/Oem/DellUpdateService.Install";
        public const string SimpleUpdate = @"/redfish/v1/UpdateService/Actions/UpdateService.SimpleUpdate";
        #endregion

        public UpdateController(Server server)
            :base(server)
        { }

        /// <summary>
        /// Realiza um update de firmware em um servidor que suporta Redfish
        /// </summary>
        /// <param name="path">Caminho completo para o arquvi do firmware</param>
        /// <param name="option">Modo de instalação</param>
        /// <returns>IdracJob</returns>
        public async Task<IdracJob> UpdateFirmwareAsync(string path, string option)
        {
            Uri location = await UploadFileAsync(path);
            return await InstallFirmwareAsync(location, option);
        }

        /// <summary>
        /// Realiza um update de varios firmwares em um servidor
        /// </summary>
        /// <param name="paths">Lista com o caminho dos recursos</param>
        /// <param name="option">Modo de instalação</param>
        /// <returns></returns>
        public async Task<IdracJob> UpdateFirmwareAsync(IEnumerable<string> paths, string option)
        {
            var locations = new List<Uri>();
            foreach(var item in paths)
            {
                locations.Add(await UploadFileAsync(item));
            }
            return await InstallFirmwareAsync(locations, option);
        }

        /// <summary>
        /// Realiza a instalação do Firmware na Idrac
        /// </summary>
        /// <param name="location">Uri do recurso</param>
        /// <param name="option">Modo de Instalação</param>
        /// <returns>Job de Instação fo Firmware</returns>
        private async Task<IdracJob> InstallFirmwareAsync(Uri location, string option)
        {
            var uris = new List<string>() { location.ToString() };
            var content = new
            {
                SoftwareIdentityURIs = uris,
                InstallUpon = option
            };
            string jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(BaseUri + FirmwareInstall, httpContent);
        }

        /// <summary>
        /// Realiza a instalação do Firmware na Idrac
        /// </summary>
        /// <param name="locations">Lista com a localização dos recursos</param>
        /// <param name="option">Modo de Instalação</param>
        /// <returns>Job de Instação fo Firmware</returns>
        private async Task<IdracJob> InstallFirmwareAsync(IEnumerable<Uri> locations, string option)
        {
            var uris = new List<string>(); 
            foreach(var item in locations)
            {
                uris.Add(item.ToString());
            }
            var content = new
            {
                SoftwareIdentityURIs = uris,
                InstallUpon = option
            };
            string jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(BaseUri + FirmwareInstall, httpContent);
        }

        /// <summary>
        /// Realiza o upload do firmware para a Idrac
        /// </summary>
        /// <param name="path">caminho completo do arquivo do firmware</param>
        /// <returns>Uri com a localização do recurso</returns>
        private async Task<Uri> UploadFileAsync(string path)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, BaseUri + FirmwareInventory))
            using (var multipartContent = new MultipartFormDataContent())
            using (var fileContent = new StreamContent(File.Open(path, FileMode.Open)))
            {
                request.Headers.Authorization = Credentials;
                string etag = await GetHeaderValueAsync("ETag", BaseUri + FirmwareInventory);
                request.Headers.TryAddWithoutValidation("If-Match", etag);
                request.Headers.CacheControl = CacheControlHeaderValue.Parse("no-cache");
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"file\"",
                    FileName = string.Format("\"{0}\"", Path.GetFileName(path))
                };
                multipartContent.Add(fileContent);
                request.Content = multipartContent;
                using (HttpResponseMessage response = await Client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException("Falha no upload do arquivo: " + response.ReasonPhrase);

                    return response.Headers.Location;
                }
            }
        }
    }
}
