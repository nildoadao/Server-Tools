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
        private static HttpClientHandler _handler;

        #region Parametros default

        private const string USER_AGENT = "Server Tools";
        private const bool PRE_AUTHENTICATE = true;

        #endregion

        public static HttpClient GetClient(string user, string password)
        {
            Init(user, password);
            return _client;
        }

        private static void Init(string user, string password)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            _handler = new HttpClientHandler();
            _handler.Credentials = new NetworkCredential(user, ConvertToSecureString(password));
            _handler.PreAuthenticate = PRE_AUTHENTICATE;
            _client = new HttpClient(_handler);
            _client.DefaultRequestHeaders.ExpectContinue = false;
            _client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        }

        private static SecureString ConvertToSecureString(string text)
        {
            SecureString secureString = new SecureString();
            foreach (char c in text.ToCharArray())
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }
    }
}
