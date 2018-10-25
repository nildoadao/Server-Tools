using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
namespace Server_Tools.Util
{
    class HttpUtil
    {
        private static HttpClient _client;

        #region Parametros default

        private const string USER_AGENT = "Server_Tools";

        #endregion

        public static HttpClient Client
        {
            get
            {
                if (_client == null)
                    BuildClient();

                return _client;
            }
        }

        private static void BuildClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _client = new HttpClient();
            //_client.DefaultRequestHeaders.ExpectContinue = true;
            _client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            _client.DefaultRequestHeaders.Connection.Add("keep-alive");
        }

        public static AuthenticationHeaderValue GetCredentialHeader(string user, string password)
        {
            var credentials = string.Format("{0}:{1}", user, password);
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials)));
        }
    }
}
