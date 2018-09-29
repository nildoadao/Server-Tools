using Newtonsoft.Json;
using Server_Tools.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
namespace Server_Tools.Util
{
    class HttpUtil
    {
        private static HttpClient _client;

        #region Parametros default

        private const string USER_AGENT = "Server Tools";

        #endregion

        public static HttpClient GetClient()
        {
            if(_client == null)
            {
                Init();
            }
            return _client;
        }

        private static void Init()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.ExpectContinue = false;
            _client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        }

        public static AuthenticationHeaderValue GetCredentialHeader(string user, string password)
        {
            var credentials = string.Format("{0}:{1}", user, password);
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials)));
        }
    }
}
