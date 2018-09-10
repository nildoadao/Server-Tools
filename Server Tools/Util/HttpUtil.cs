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

        private const string IDRAC_USER = "root";
        private const string IDRAC_PASSWORD = "calvin";
        private const string USER_AGENT = "Server Tools";
        private const bool PRE_AUTHENTICATE = true;

        #endregion

        /// <summary>
        /// Retorna uma instancia HttpClient 
        /// </summary>
        /// <returns></returns>
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
            // Desabilitar validação de Cert. SSL
            _handler = new HttpClientHandler();
            _handler.Credentials = new NetworkCredential(IDRAC_USER, ConvertToSecureString(IDRAC_PASSWORD));
            _handler.PreAuthenticate = PRE_AUTHENTICATE;
            _client = new HttpClient(_handler);
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
