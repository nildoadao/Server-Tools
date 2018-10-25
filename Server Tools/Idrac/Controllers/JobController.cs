using Server_Tools.Idrac.Models;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class JobController : BaseIdrac
    {
        # region Redfish Uris
        public const string JobStatus = @"/redfish/v1/Managers/iDRAC.Embedded.1/Jobs/";
        public const string JobResult = @"/redfish/v1/TaskService/Tasks/";
        #endregion

        public const double JobTimeout = 10;

        public JobController(Server server)
            :base(server)
        { }

        /// <summary>
        /// Cria um Job na Idrac utilizando o metodo POST
        /// </summary>
        /// <param name="uri">String contento o enderço do recurso</param>
        /// <param name="content">Conteudo Http da requisição</param>
        /// <returns>String contendo o Job Id</returns>
        public async Task<IdracJob> CreateJob(string uri, HttpContent content)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Authorization = credentials;
                request.Content = content;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao criar Job: {0}", response.ReasonPhrase));

                    string location = response.Headers.Location.ToString();
                    return await GetResource<IdracJob>(baseUri + location);
                }
            }
        }

        /// <summary>
        /// Cria um Job para requisições que não precisam de conteudo.
        /// </summary>
        /// <param name="uri">Uri do recurso</param>
        /// <param name="method">Método HTTP da requisição</param>
        /// <returns></returns>
        public async Task<IdracJob> CreateJob(string uri, HttpMethod method)
        {
            using (var request = new HttpRequestMessage(method, uri))
            {
                request.Headers.Authorization = credentials;;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao criar Job: {0}", response.ReasonPhrase));

                    string location = response.Headers.Location.ToString();
                    return await GetResource<IdracJob>(baseUri + location);
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
            return await GetResource<IdracJob>(baseUri + JobStatus + jobId);
        }

        /// <summary>
        /// Retorna os dados de um Job
        /// </summary>
        /// <param name="jobId">Identificação do Job</param>
        /// <returns>Resposta Http do Job</returns>
        public async Task<HttpResponseMessage> GetJobData(string jobId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, baseUri + JobResult + jobId))
            {
                request.Headers.Authorization = credentials;
                return await client.SendAsync(request);
            }
        }
    }
}
