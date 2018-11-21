using Server_Tools.Idrac.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class JobController : BaseIdrac
    {
        // Uris contendo a localização dos recursos de Job
        public const string JobStatus = @"/redfish/v1/Managers/iDRAC.Embedded.1/Jobs/";
        public const string JobResult = @"/redfish/v1/TaskService/Tasks/";

        // Timeout para execução de Um Job
        public const double JobTimeout = 10;

        /// <summary>
        /// Cria uma nova instacia de JobController
        /// </summary>
        /// <param name="server">Objeto contendo os dados do servidor</param>
        public JobController(Server server)
            :base(server)
        { }

        /// <summary>
        /// Cria um Job na Idrac utilizando o metodo POST
        /// </summary>
        /// <param name="uri">String contento o enderço do recurso</param>
        /// <param name="content">Conteudo Http da requisição</param>
        /// <returns>String contendo o Job Id</returns>
        public async Task<IdracJob> CreateJobAsync(string uri, HttpContent content)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Authorization = Credentials;
                request.Content = content;
                using (var response = await Client.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao criar Job: {0}", response.ReasonPhrase));

                    string location = response.Headers.Location.ToString();
                    return await GetResourceAsync<IdracJob>(BaseUri + location);
                }
            }
        }

        /// <summary>
        /// Cria um Job para requisições que não precisam de conteudo.
        /// </summary>
        /// <param name="uri">Uri do recurso</param>
        /// <param name="method">Método HTTP da requisição</param>
        /// <returns></returns>
        public async Task<IdracJob> CreateJobAsync(string uri, HttpMethod method)
        {
            using (var request = new HttpRequestMessage(method, uri))
            {
                request.Headers.Authorization = Credentials;;
                using (var response = await Client.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao criar Job: {0}", response.ReasonPhrase));

                    string location = response.Headers.Location.ToString();
                    return await GetResourceAsync<IdracJob>(BaseUri + location);
                }
            }
        }

        /// <summary>
        /// Retorna um objeto contendo os dados do Job da Idrac
        /// </summary>
        /// <param name="jobId">Identificação do Job</param>
        /// <returns>O Job corresponde ao ID</returns>
        public async Task<IdracJob> GetJobAsync(string jobId)
        {
            return await GetResourceAsync<IdracJob>(BaseUri + JobStatus + jobId);
        }

        /// <summary>
        /// Retorna os dados de um Job
        /// </summary>
        /// <param name="jobId">Identificação do Job</param>
        /// <returns>Resposta Http do Job</returns>
        public async Task<HttpResponseMessage> GetJobDataAsync(string jobId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, BaseUri + JobResult + jobId))
            {
                request.Headers.Authorization = Credentials;
                return await Client.SendAsync(request);
            }
        }
    }
}
