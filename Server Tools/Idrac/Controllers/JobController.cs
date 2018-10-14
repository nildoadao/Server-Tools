using Server_Tools.Idrac.Models;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class JobController : BaseIdrac
    {
        # region Uri
        public const string JOB_STATUS = @"/redfish/v1/Managers/iDRAC.Embedded.1/Jobs/";
        public const string JOB_RESULT = @"/redfish/v1/TaskService/Tasks/";
        #endregion

        public JobController(Server server)
            :base(server)
        { }

        /// <summary>
        /// Cria um Job na Idrac utilizando o metodo POST
        /// </summary>
        /// <param name="uri">String contento o enderço do recurso</param>
        /// <param name="content">Conteudo Http da requisição</param>
        /// <returns>String contendo o Job Id</returns>
        public async Task<string> CreateJob(string uri, HttpContent content)
        {
            return await CreateJob(uri, content, HttpMethod.Post);
        }

        /// <summary>
        /// Cria um Job na Idrac
        /// </summary>
        /// <param name="uri">String contento o enderço do recurso</param>
        /// <param name="content">Conteudo Http da requisição</param>
        /// <param name="method">Metodo Http usado</param>
        /// <returns>String contendo o Job Id</returns>
        public async Task<string> CreateJob(string uri, HttpContent content, HttpMethod method)
        {
            using (var request = new HttpRequestMessage(method, uri))
            {
                request.Headers.Authorization = credentials;
                request.Content = content;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao criar Job: {0}", response.ReasonPhrase));

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

        public async Task<HttpResponseMessage> GetJobData(string jobId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, baseUri + JOB_RESULT + jobId))
            {
                request.Headers.Authorization = credentials;
                return await client.SendAsync(request);
            }
        }
    }
}
