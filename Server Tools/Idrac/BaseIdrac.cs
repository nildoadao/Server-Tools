using Newtonsoft.Json;
using Server_Tools.Model;
using Server_Tools.Util;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Server_Tools.Idrac
{
    abstract class BaseIdrac
    {
        protected Server server;
        protected string baseUri;
        protected HttpClient client;
        protected AuthenticationHeaderValue credentials;

        public BaseIdrac(Server server)
        {
            this.server = server;
            baseUri = String.Format(@"https://{0}", server.Host);
            client = HttpUtil.Client;
            credentials = HttpUtil.GetCredentialHeader(server.User, server.Password);
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
                request.Headers.Authorization = credentials;
                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                        support = true;
                }
                return support;
            }
        }

        /// <summary>
        /// Retorna um objeto genérico a partir de uma Uri da API Redfish
        /// </summary>
        /// <typeparam name="T">Tipo do Objeto de retorno</typeparam>
        /// <param name="uri">Localização do recurso</param>
        /// <returns></returns>
        public async Task<T> GetResource<T>(string uri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Authorization = credentials;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao obter recurso {0} {1}", uri, response.ReasonPhrase));

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
                request.Headers.Authorization = credentials;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao obter o cabeçalho {0} {1}", header, response.ReasonPhrase));

                    string result = "";

                    foreach (string item in response.Headers.GetValues(header))
                        result = item;

                    return result;
                }
            }
        }
    }
}
