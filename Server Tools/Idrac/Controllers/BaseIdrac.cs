using Newtonsoft.Json;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    abstract class BaseIdrac
    {
        private Server server;
        private string baseUri;
        private HttpClient client;
        private AuthenticationHeaderValue credentials;

        /// <summary>
        /// Objeto contendo os dados basicos do servidor.
        /// </summary>
        protected Server Server
        {
            get { return server; }
        }

        /// <summary>
        ///  Url de base para as requisições.
        /// </summary>
        protected string BaseUri
        {
            get { return baseUri; }
        }

        /// <summary>
        /// Client Http utilizado nas requisições.
        /// </summary>
        protected HttpClient Client
        {
            get { return client; }
        }

        /// <summary>
        /// Cabeçalho padrão de autenticação.
        /// </summary>
        protected AuthenticationHeaderValue Credentials
        {
            get { return credentials; }
        }

        /// <summary>
        /// Inicializa uma nova instancia de BaseIdrac
        /// </summary>
        /// <param name="server">Clase contendo os dados basicos do servidor</param>
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
        public async Task<bool> CheckRedfishSupportAsync(string resource)
        {
            bool support = false;
            using (var request = new HttpRequestMessage(HttpMethod.Get, baseUri + resource))
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
        public async Task<T> GetResourceAsync<T>(string uri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Authorization = credentials;
                using (var response = await client.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

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
        /// <returns>Lista com os valores do cabeçalho</returns>
        public async Task<IList<string>> GetHeaderValueAsync(string header, string uri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Authorization = credentials;
                using (var response = await client.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao obter o cabeçalho {0} {1}", header, response.ReasonPhrase));

                    List<string> values = new List<string>();
                    foreach(var item in response.Headers.GetValues(header))
                    {
                        values.Add(item);
                    }
                    return values;                   
                }
            }
        }
    }
}
